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
            string connectionString = tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC";

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString(connectionString)))
                {
                    connection.Open();
                    _logger.LogInformation("Connessione al database aperta con successo per LeggiModuloTesto");

                    var sql = @"SELECT * FROM IC6DRSF0 WHERE IC6OGG = @cOggetto AND IC6TMO = @cTipo ORDER BY IC6DAT DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@cOggetto", cOggetto);
                        command.Parameters.AddWithValue("@cTipo", cTipo);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();
                                var appData = reader["IC6DAT"];
                                _logger.LogInformation("Dati trovati per LeggiModuloTesto: Oggetto = {COggetto}, Tipo = {CTipo}", cOggetto, cTipo);

                                do
                                {
                                    if (Convert.ToDouble(appData) == Convert.ToDouble(reader["IC6DAT"]))
                                    {
                                        if (!string.IsNullOrWhiteSpace(reader["IC6TES"].ToString()))
                                        {
                                            altre += reader["IC6TES"].ToString().Trim() + Environment.NewLine;
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
            string connectionString = tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC";

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString(connectionString)))
                {
                    connection.Open();
                    _logger.LogInformation("Connessione al database aperta con successo per LeggiEredi");

                    var sql = @"SELECT TOP 300 IBARS1, IBARS2, IBADNA, IBACIN, IBACON,
                                (SELECT TOP 1 PROV FROM TAB_COMUNI WHERE Comune = IBACON) AS PROVNA,
                                IBANAN, T2.TBBCA1 AS NAZNAS, IBAIND, T4.TBBDES AS TIPOIND, IBADEI, IBACII, IBACAP, IBAIST, IBACIT, IBAPRV, IBANAZ, T5.TBBCA1 AS NAZNAZ, T1.TBBDES AS DESCARICA, IBFNUM AS CFEREDE
                                FROM IBOESPF0
                                INNER JOIN TBBTABF0 AS T1 ON T1.TBBTTA = 'CAT01' AND T1.TBBCTA = IBOCCA AND T1.TBBCLI = 'IT'
                                INNER JOIN IBAOGGF0 ON IBACOG = IBOCES
                                LEFT JOIN IBFREGF0 ON IBFOGG = IBOCES AND IBFTRE = 'FIS'
                                LEFT JOIN TBBTABF0 AS T2 ON T2.TBBTTA = 'NAZ' AND T2.TBBCTA = IBANAN AND T2.TBBCLI = 'IT'
                                LEFT JOIN TBBTABF0 AS T4 ON T4.TBBTTA = 'IND' AND T4.TBBCTA = IBAIND AND T4.TBBCLI = 'IT'
                                LEFT JOIN TBBTABF0 AS T5 ON T5.TBBTTA = 'NAZ' AND T5.TBBCTA = IBANAZ AND T5.TBBCLI = 'IT'
                                WHERE IBOOGG = @oggetto AND IBOFLC = ' ' ORDER BY IBOFLC, IBADNA ASC";

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
                                    var dna = reader["IBADNA"].ToString();
                                    if (!string.IsNullOrWhiteSpace(dna) && dna.Length == 8)
                                    {
                                        dna = $"{dna.Substring(6, 2)}/{dna.Substring(4, 2)}/{dna.Substring(0, 4)}";
                                        note = $"Nato/a il {dna}";
                                    }

                                    var con = reader["IBACON"].ToString();
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
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["IBARS1"]} {reader["IBARS2"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["TIPOIND"]} {reader["IBADEI"]} {reader["IBACII"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["IBACAP"]} {reader["IBACIT"]}</font></td>");
                                    htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["IBAPRV"]}</font></td>");
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
            string connectionString = tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC";

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString(connectionString)))
                {
                    connection.Open();
                    _logger.LogInformation("Connessione al database aperta con successo per AggiornaFileCorrispondenti");

                    var countSql = @"SELECT COUNT(*) FROM PBSACOF0 
                                     WHERE PBSAPR = @annoProt AND PBSNPR = @numeroProt AND PBSACC = @codAcc 
                                     AND PBSURG = @codUrg AND PBSNCO = @nazCor AND PBSCCO = @codCor";

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

                            var updateSql = @"UPDATE PBSACOF0 
                                              SET PBSSCARICO = @scarico, PBSFILE = @file 
                                              WHERE PBSAPR = @annoProt AND PBSNPR = @numeroProt 
                                              AND PBSACC = @codAcc AND PBSURG = @codUrg 
                                              AND PBSNCO = @nazCor AND PBSCCO = @codCor";

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
