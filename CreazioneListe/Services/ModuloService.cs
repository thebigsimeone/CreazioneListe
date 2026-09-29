using CreazioneListe.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CreazioneListe.Services
{
    public class ModuloService : IModuloService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ModuloService> _logger;

        public ModuloService(IConfiguration configuration, ILogger<ModuloService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string LeggiModuloTesto(string cOggetto, string cTipo, string tenant)
        {
            var altre = string.Empty;
            string connectionString = TenantConfiguration.GetConnectionString(_configuration, tenant);

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    _logger.LogInformation("Connessione al database aperta con successo per LeggiModuloTesto");

                    var sql = @"SELECT * FROM Annotazioni WHERE AnnotazioneSoggetto = @cOggetto AND TipoAnnotazione = @cTipo ORDER BY DataAnnotazione DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@cOggetto", cOggetto);
                        command.Parameters.AddWithValue("@cTipo", cTipo);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();
                                var appData = reader["DataAnnotazione"];
                                _logger.LogInformation("Dati trovati per LeggiModuloTesto: Oggetto = {COggetto}, Tipo = {CTipo}", cOggetto, cTipo);

                                do
                                {
                                    if (Convert.ToDouble(appData) == Convert.ToDouble(reader["DataAnnotazione"]))
                                    {
                                        if (!string.IsNullOrWhiteSpace(reader["TestoAnnotazione"].ToString()))
                                        {
                                            altre += reader["TestoAnnotazione"].ToString().Trim() + Environment.NewLine;
                                        }
                                    }
                                } while (reader.Read());
                            }
                            else
                            {
                                _logger.LogWarning("Nessun dato trovato per LeggiModuloTesto: Oggetto = {COggetto}, Tipo = {CTipo}", cOggetto, cTipo);
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Errore SQL durante l'esecuzione di LeggiModuloTesto per Oggetto = {COggetto}, Tipo = {CTipo}", cOggetto, cTipo);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico durante l'esecuzione di LeggiModuloTesto per Oggetto = {COggetto}, Tipo = {CTipo}", cOggetto, cTipo);
                throw;
            }

            return altre;
        }

        public string LeggiEredi(string oggetto, string primoRigo, string tenant)
        {
            var htEredi = new StringBuilder();
            string connectionString = TenantConfiguration.GetConnectionString(_configuration, tenant);

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    _logger.LogInformation("Connessione al database aperta con successo per LeggiEredi");

                    var sql = @"SELECT TOP 300 CognomeRagioneSociale, NomeSoggetto, DataNascita, CodiceComuneNascita, ComuneNascita,
                                (SELECT TOP 1 PROV FROM Comuni WHERE Comune = ComuneNascita) AS PROVNA,
                                NazioneNascita, T2.ValoreDecodifica AS NAZNAS, TipoIndirizzo, T4.DescrizioneDecodifica AS TIPOIND, Indirizzo, NumeroCivico, Cap, CodiceComuneResidenza, ComuneResidenza, ProvinciaResidenza, NazioneResidenza, T5.ValoreDecodifica AS NAZNAZ, T1.DescrizioneDecodifica AS DESCARICA, ValoreIdentificativo AS CFEREDE
                                FROM RelazioniSoggetti
                                INNER JOIN Decodifiche AS T1 ON T1.TipoDecodifica = 'CAT01' AND T1.CodiceDecodifica = TipoRelazione AND T1.Lingua = 'IT'
                                INNER JOIN Soggetti ON CodiceSoggetto = SoggettoCollegato
                                LEFT JOIN IdentificativiSoggetti ON IdentificativoSoggetto = SoggettoCollegato AND TipoIdentificativo = 'FIS'
                                LEFT JOIN Decodifiche AS T2 ON T2.TipoDecodifica = 'NAZ' AND T2.CodiceDecodifica = NazioneNascita AND T2.Lingua = 'IT'
                                LEFT JOIN Decodifiche AS T4 ON T4.TipoDecodifica = 'IND' AND T4.CodiceDecodifica = TipoIndirizzo AND T4.Lingua = 'IT'
                                LEFT JOIN Decodifiche AS T5 ON T5.TipoDecodifica = 'NAZ' AND T5.CodiceDecodifica = NazioneResidenza AND T5.Lingua = 'IT'
                                WHERE SoggettoOrigine = @oggetto AND Annullata = ' ' ORDER BY Annullata, DataNascita ASC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@oggetto", oggetto);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                _logger.LogInformation("Dati trovati per LeggiEredi: Oggetto = {Oggetto}", oggetto);
                                var rr = 0;

                                while (reader.Read())
                                {
                                    var note = string.Empty;
                                    var dna = reader["DataNascita"].ToString();
                                    if (!string.IsNullOrWhiteSpace(dna) && dna.Length == 8)
                                    {
                                        dna = $"{dna.Substring(6, 2)}/{dna.Substring(4, 2)}/{dna.Substring(0, 4)}";
                                        note = $"Nato/a il {dna}";
                                    }

                                    var con = reader["ComuneNascita"].ToString();
                                    if (!string.IsNullOrWhiteSpace(con) && con.Length > 8)
                                    {
                                        note += $" a {con} ({reader["PROVNA"]}) {reader["NAZNAS"]}";
                                    }

                                    rr++;
                                    var sx = rr == 1 ? primoRigo : "<td width='10%' colspan='8' align='LEFT'><font face='verdana' size='2' color='navy'>&nbsp;</font></td>";

                                    htEredi.AppendLine("<tr>");
                                    htEredi.AppendLine(sx);
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["DESCARICA"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["CFEREDE"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["CognomeRagioneSociale"]} {reader["NomeSoggetto"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["TIPOIND"]} {reader["Indirizzo"]} {reader["NumeroCivico"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["Cap"]} {reader["ComuneResidenza"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["ProvinciaResidenza"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{note}</font></td>");
                                    htEredi.AppendLine("</tr>");
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Nessun dato trovato per LeggiEredi: Oggetto = {Oggetto}", oggetto);
                                htEredi.AppendLine("<tr>");
                                htEredi.AppendLine(primoRigo);
                                for (int i = 0; i < 7; i++)
                                {
                                    htEredi.AppendLine("<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>&nbsp;</font></td>");
                                }
                                htEredi.AppendLine("</tr>");
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Errore SQL durante l'esecuzione di LeggiEredi per Oggetto = {Oggetto}", oggetto);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico durante l'esecuzione di LeggiEredi per Oggetto = {Oggetto}", oggetto);
                throw;
            }

            return htEredi.ToString();
        }

        public void AggiornaFileCorrispondenti(int annoProt, int numeroProt, string codAcc, string codUrg, string nazCor, string codCor, string oFile, string tenant)
        {
            string connectionString = TenantConfiguration.GetConnectionString(_configuration, tenant);

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    _logger.LogInformation("Connessione al database aperta con successo per AggiornaFileCorrispondenti");

                    var countSql = @"SELECT COUNT(*) FROM Assegnazioni 
                                     WHERE PraticaAnno = @annoProt AND PraticaNumero = @numeroProt AND AccertamentoCodice = @codAcc 
                                     AND UrgenzaCodice = @codUrg AND FornitoreNazione = @nazCor AND FornitoreCodice = @codCor";

                    using (var countCommand = new SqlCommand(countSql, connection))
                    {
                        countCommand.Parameters.AddWithValue("@annoProt", annoProt);
                        countCommand.Parameters.AddWithValue("@numeroProt", numeroProt);
                        countCommand.Parameters.AddWithValue("@codAcc", codAcc);
                        countCommand.Parameters.AddWithValue("@codUrg", codUrg);
                        countCommand.Parameters.AddWithValue("@nazCor", nazCor);
                        countCommand.Parameters.AddWithValue("@codCor", codCor);

                        int count = (int)countCommand.ExecuteScalar();
                        if (count > 0)
                        {
                            _logger.LogInformation("Record trovato per AggiornaFileCorrispondenti: AnnoProt = {AnnoProt}, NumeroProt = {NumeroProt}", annoProt, numeroProt);

                            var updateSql = @"UPDATE Assegnazioni 
                                              SET Esportata = @scarico, NomeFileEsportazione = @file 
                                              WHERE PraticaAnno = @annoProt AND PraticaNumero = @numeroProt 
                                              AND AccertamentoCodice = @codAcc AND UrgenzaCodice = @codUrg 
                                              AND FornitoreNazione = @nazCor AND FornitoreCodice = @codCor";

                            using (var updateCommand = new SqlCommand(updateSql, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@scarico", "S");
                                updateCommand.Parameters.AddWithValue("@file", oFile);
                                updateCommand.Parameters.AddWithValue("@annoProt", annoProt);
                                updateCommand.Parameters.AddWithValue("@numeroProt", numeroProt);
                                updateCommand.Parameters.AddWithValue("@codAcc", codAcc);
                                updateCommand.Parameters.AddWithValue("@codUrg", codUrg);
                                updateCommand.Parameters.AddWithValue("@nazCor", nazCor);
                                updateCommand.Parameters.AddWithValue("@codCor", codCor);

                                updateCommand.ExecuteNonQuery();
                                _logger.LogInformation("Aggiornamento effettuato con successo per AggiornaFileCorrispondenti: AnnoProt = {AnnoProt}, NumeroProt = {NumeroProt}", annoProt, numeroProt);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Nessun record trovato per AggiornaFileCorrispondenti: AnnoProt = {AnnoProt}, NumeroProt = {NumeroProt}", annoProt, numeroProt);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Errore SQL durante l'esecuzione di AggiornaFileCorrispondenti per AnnoProt = {AnnoProt}, NumeroProt = {NumeroProt}", annoProt, numeroProt);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico durante l'esecuzione di AggiornaFileCorrispondenti per AnnoProt = {AnnoProt}, NumeroProt = {NumeroProt}", annoProt, numeroProt);
                throw;
            }
        }
    }
}

