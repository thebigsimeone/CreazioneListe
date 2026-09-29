using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;

namespace CreazioneListe.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<DataTable> GetSelectAsync(FormData formData, string tenant)
        {
            var dataTable = new DataTable();
            string connectionString = TenantConfiguration.GetConnectionString(_configuration, tenant);

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT FornitoreNazione, FornitoreCodice, RagioneSocialeFornitore, AccertamentoCodice, UrgenzaCodice, Importo, DescrizioneAccertamento, Count(*) AS TotAcc 
                                  FROM Assegnazioni
                                  INNER JOIN Pratiche ON AnnoProtocollo = PraticaAnno AND NumeroProtocollo = PraticaNumero
                                  INNER JOIN Fornitori ON NazioneFornitore = FornitoreNazione AND CodiceFornitore = FornitoreCodice AND TipoFornitore = 'D'
                                  INNER JOIN TipiAccertamento ON CodiceAccertamento = AccertamentoCodice
                                  WHERE DataAffidamento >= @DaDataAff AND DataAffidamento <= @DataAff 
                                  AND StatoAssegnazione = '2' AND Esportata <> 'S' AND TipoPratica <> 'SCO'
                                  GROUP BY FornitoreNazione, FornitoreCodice, RagioneSocialeFornitore, AccertamentoCodice, UrgenzaCodice, Importo, DescrizioneAccertamento
                                  ORDER BY FornitoreNazione, FornitoreCodice, AccertamentoCodice, Importo";

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddDateParameters(command, formData);
                        AddClientExclusions(command);

                        if (!string.IsNullOrEmpty(formData?.CodAcc))
                        {
                            command.Parameters.AddWithValue("@CodAcc", formData.CodAcc);
                        }
                        if (!string.IsNullOrEmpty(formData?.NumLotto))
                        {
                            command.Parameters.AddWithValue("@NumLotto", Convert.ToInt32(formData.NumLotto));
                        }

                        var adapter = new SqlDataAdapter(command);
                        await Task.Run(() => adapter.Fill(dataTable));
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Errore SQL durante l'esecuzione di GetSelectAsync per tenant {Tenant}.", tenant);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esecuzione di GetSelectAsync per tenant {Tenant}.", tenant);
                throw;
            }

            return dataTable;
        }


        public async Task<DataTable> GetSelectedAsync(RichiestaExcel richiestaExcel, FormData formData, string tenant)
        {
            var dataTable = new DataTable();
            string connectionString = TenantConfiguration.GetConnectionString(_configuration, tenant);

            try
            {
                _logger.LogInformation("Avvio della connessione al database per tenant {Tenant}.", tenant);

                using (var connection = new SqlConnection(connectionString))
                {
                    // Costruzione dinamica della query
                    var queryBuilder = new StringBuilder();
                    queryBuilder.Append(@"
                                            SELECT ISNULL(T1.DescrizioneDecodifica, '') AS TipoIND, 
                                                   ISNULL(T2.DescrizioneDecodifica, '') AS TipoCONT, 
                                                   Pratiche.AnnoProtocollo, Pratiche.NumeroProtocollo, Pratiche.ClienteCodice,
                                                   Pratiche.SoggettoCodice, Pratiche.CodiceFiscale, Pratiche.PartitaIva,
                                                   Pratiche.Denominazione, Pratiche.ComunePratica,
                                                   Soggetti.CodiceSoggetto, Soggetti.CognomeRagioneSociale, Soggetti.NomeSoggetto,
                                                   Soggetti.Indirizzo, Soggetti.NumeroCivico, Soggetti.Cap,
                                                   Soggetti.ComuneResidenza, Soggetti.ProvinciaResidenza,
                                                   DatoriLavoro.CodiceFiscaleDatore");

                    // Se CodCor è "VUT", includi Annotazioni
                    if (richiestaExcel.CodAcc == "VUT")
                    {
                        queryBuilder.Append(@", Annotazioni.TestoAnnotazione");
                    }

                    queryBuilder.Append(@"
                                            FROM Assegnazioni
                                            INNER JOIN Pratiche ON AnnoProtocollo = PraticaAnno AND NumeroProtocollo = PraticaNumero
                                            LEFT JOIN Soggetti ON CodiceSoggetto = SoggettoCodice
                                            LEFT JOIN Decodifiche AS T1 ON T1.TipoDecodifica = 'IND' AND T1.Lingua = 'IT' AND T1.CodiceDecodifica = TipoIndirizzo
                                            LEFT JOIN DatoriLavoro ON LavoratoreCodice = SoggettoCodice
                                            LEFT JOIN Decodifiche AS T2 ON T2.TipoDecodifica = 'DCO' AND T2.Lingua = 'IT' AND T2.CodiceDecodifica = TipoContratto");

                    // Se CodCor è "VUT", aggiungi la join con Annotazioni e il filtro sulla colonna TestoAnnotazione
                    if (richiestaExcel.CodAcc == "VUT")
                    {
                        queryBuilder.Append(@"
                                            LEFT JOIN Annotazioni ON AnnotazioneSoggetto = SoggettoCodice
                                            AND TipoAnnotazione = 'TEL' 
                                            AND TestoAnnotazione LIKE 'TEL.:%' 
                                            AND TestoAnnotazione NOT LIKE '%ATTIVO%'");
                    }

                    queryBuilder.Append(@"
                                            WHERE DataAffidamento >= @DaDataAff 
                                            AND DataAffidamento <= @DataAff 
                                            AND StatoAssegnazione = '2' 
                                            AND Esportata <> 'S' 
                                            AND FornitoreNazione = @NazCor 
                                            AND FornitoreCodice = @CodCor 
                                            AND AccertamentoCodice = @CodAcc 
                                            AND UrgenzaCodice = @CodUrg 
                                            AND TipoPratica <> 'SCO'");

                    string query = queryBuilder.ToString();

                    using (var command = new SqlCommand(query, connection))
                    {
                        AddDateParameters(command, formData);
                        AddClientExclusions(command);
                        command.Parameters.AddWithValue("@NazCor", formData.NazCor ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CodCor", Convert.ToInt32(formData.CodCor));
                        command.Parameters.AddWithValue("@CodAcc", formData.CodAcc ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CodUrg", richiestaExcel.CodUrg ?? (object)DBNull.Value);

                        var adapter = new SqlDataAdapter(command);
                        await Task.Run(() => adapter.Fill(dataTable));
                        _logger.LogInformation("Query eseguita correttamente. Numero di righe restituite: {RowCount}", dataTable.Rows.Count);
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Errore SQL durante l'esecuzione di GetSelectedAsync per tenant {Tenant}.", tenant);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esecuzione di GetSelectedAsync per tenant {Tenant}.", tenant);
                throw;
            }

            return dataTable;
        }
        private static void AddDateParameters(SqlCommand command, FormData formData)
        {
            if (formData == null ||
                !DateTime.TryParseExact(formData.DaDataAff, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var from) ||
                !DateTime.TryParseExact(formData.DataAff, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var to) || from > to)
                throw new ArgumentException("Intervallo date non valido: usare yyyyMMdd.", nameof(formData));
            command.Parameters.Add("@DaDataAff", SqlDbType.Int).Value = int.Parse(formData.DaDataAff);
            command.Parameters.Add("@DataAff", SqlDbType.Int).Value = int.Parse(formData.DataAff);
        }

        private void AddClientExclusions(SqlCommand command)
        {
            var codes = _configuration.GetSection("QueryFilters:ExcludedClientCodes").Get<int[]>() ?? Array.Empty<int>();
            if (codes.Length == 0) return;
            var names = new List<string>();
            for (var i = 0; i < codes.Length; i++)
            {
                var name = "@ExcludedClient" + i;
                command.Parameters.Add(name, SqlDbType.Int).Value = codes[i];
                names.Add(name);
            }
            var filter = " AND Pratiche.ClienteCodice NOT IN (" + string.Join(", ", names) + ") ";
            var groupIndex = command.CommandText.IndexOf("GROUP BY", StringComparison.Ordinal);
            command.CommandText = groupIndex >= 0 ? command.CommandText.Insert(groupIndex, filter) : command.CommandText + filter;
        }

    }
}
