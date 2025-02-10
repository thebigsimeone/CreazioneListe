using Azure.Messaging;
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
            string connectionString = _configuration.GetConnectionString(tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var query = @"SELECT PBSNCO, PBSCCO, KBARA1, PBSACC, PBSURG, PBSVALORE, TBIDEC, Count(*) AS TotAcc 
                                  FROM PBSACOF0
                                  INNER JOIN PBARDGF0 ON PBAANP = PBSAPR AND PBANUP = PBSNPR
                                  INNER JOIN KBACORF0 ON KBANAZ = PBSNCO AND KBAPRG = PBSCCO AND KBATIPO = 'D'
                                  INNER JOIN TBIACCF0 ON TBICAC = PBSACC
                                  WHERE PBSDAF >= @DaDataAff AND PBSDAF <= @DataAff 
                                  AND PBSPAC = '2' AND PBSSCARICO <> 'S' AND PBACOP <> 'SCO'
                                  AND (PBACLI NOT IN (16643, 16644, 16645, 16646, 16698, 16699, 18011))
                                  GROUP BY PBSNCO, PBSCCO, KBARA1, PBSACC, PBSURG, PBSVALORE, TBIDEC
                                  ORDER BY PBSNCO, PBSCCO, PBSACC, PBSVALORE";

                    using (var command = new SqlCommand(query, connection))
                    {
                        var daDataAff = DateTime.TryParseExact(formData?.DaDataAff, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime daDataAffParsed) ? daDataAffParsed : DateTime.MinValue;
                        var dataAff = DateTime.TryParseExact(formData?.DataAff, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dataAffParsed) ? dataAffParsed : DateTime.MaxValue;

                        command.Parameters.AddWithValue("@DaDataAff", daDataAffParsed.ToString("yyyyMMdd"));
                        command.Parameters.AddWithValue("@DataAff", dataAffParsed.ToString("yyyyMMdd"));

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
            string connectionString = tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC";

            try
            {
                _logger.LogInformation("Avvio della connessione al database per tenant {Tenant}.", tenant);

                using (var connection = new SqlConnection(_configuration.GetConnectionString(connectionString)))
                {
                    // Costruzione dinamica della query
                    var queryBuilder = new StringBuilder();
                    queryBuilder.Append(@"
                                            SELECT ISNULL(T1.TBBDES, '') AS TipoIND, 
                                                   ISNULL(T2.TBBDES, '') AS TipoCONT, 
                                                   PBARDGF0.*, 
                                                   IBAOGGF0.*, 
                                                   DATORILAV.*");

                    // Se CodCor è "VUT", includi IC6DRSF0
                    if (richiestaExcel.CodAcc == "VUT")
                    {
                        queryBuilder.Append(@", IC6DRSF0.*");
                    }

                    queryBuilder.Append(@"
                                            FROM PBSACOF0
                                            INNER JOIN PBARDGF0 ON PBAANP = PBSAPR AND PBANUP = PBSNPR
                                            LEFT JOIN IBAOGGF0 ON IBACOG = PBAOGG
                                            LEFT JOIN TBBTABF0 AS T1 ON T1.TBBTTA = 'IND' AND T1.TBBCLI = 'IT' AND T1.TBBCTA = IBAIND
                                            LEFT JOIN DATORILAV ON LAVOGG = PBAOGG
                                            LEFT JOIN TBBTABF0 AS T2 ON T2.TBBTTA = 'DCO' AND T2.TBBCLI = 'IT' AND T2.TBBCTA = LAVCONT");

                    // Se CodCor è "VUT", aggiungi la join con IC6DRSF0 e il filtro sulla colonna IC6TES
                    if (richiestaExcel.CodAcc == "VUT")
                    {
                        queryBuilder.Append(@"
                                            LEFT JOIN IC6DRSF0 ON IC6OGG = PBAOGG
                                            AND IC6TMO = 'TEL' 
                                            AND IC6TES LIKE 'TEL.:%' 
                                            AND IC6TES NOT LIKE '%ATTIVO%'");
                    }

                    queryBuilder.Append(@"
                                            WHERE PBSDAF >= @DaDataAff 
                                            AND PBSDAF <= @DataAff 
                                            AND PBSPAC = '2' 
                                            AND PBSSCARICO <> 'S' 
                                            AND PBSNCO = @NazCor 
                                            AND PBSCCO = @CodCor 
                                            AND PBSACC = @CodAcc 
                                            AND PBSURG = @CodUrg 
                                            AND PBACOP <> 'SCO'");

                    string query = queryBuilder.ToString();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@DaDataAff", Convert.ToInt32(formData.DaDataAff));
                        command.Parameters.AddWithValue("@DataAff", Convert.ToInt32(formData.DataAff));
                        command.Parameters.AddWithValue("@NazCor", formData.NazCor ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CodCor", Convert.ToInt32(formData.CodCor));
                        command.Parameters.AddWithValue("@CodAcc", formData.CodAcc ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CodUrg", richiestaExcel.CodUrg ?? (object)DBNull.Value);

                        _logger.LogInformation("Esecuzione della query per tenant {Tenant}: {Query}", tenant, query);
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
    }
}
