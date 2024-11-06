using CreazioneListe.Interfaces;
using Microsoft.Data.SqlClient;
using System.Text;

namespace CreazioneListe.Services
{
    public class ModuloService : IModuloService
    {
        private readonly IConfiguration _configuration;

        public ModuloService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string LeggiModuloTesto(string cOggetto, string cTipo, string tenant)
        {
            var altre = string.Empty;
            string connectionString = tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(connectionString)))
            {
                connection.Open();
                var sql = $"SELECT * FROM IC6DRSF0 WHERE IC6OGG = @cOggetto AND IC6TMO = @cTipo ORDER BY IC6DAT DESC";
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
                    }
                }
            }
            return altre;
        }

        public string LeggiEredi(string oggetto, string primoRigo, string tenant)
        {
            var htEredi = new StringBuilder();
            string connectionString = tenant == "EBI" ? "DefaultConnection_EBI" : "DefaultConnection_SSC";

            using (var connection = new SqlConnection(_configuration.GetConnectionString(connectionString)))
            {
                connection.Open();
                var sql = "SELECT Top 300 IBARS1, IBARS2, IBADNA, IBACIN, IBACON, " +
                          "(SELECT Top 1 PROV From TAB_COMUNI Where Comune = IBACON) AS PROVNA, " +
                          "IBANAN, T2.TBBCA1 AS NAZNAS, IBAIND, T4.TBBDES AS TIPOIND, IBADEI, IBACII, IBACAP, IBAIST, IBACIT, IBAPRV, IBANAZ, T5.TBBCA1 AS NAZNAZ, T1.TBBDES AS DESCARICA, IBFNUM as CFEREDE " +
                          "FROM IBOESPF0 " +
                          "INNER JOIN TBBTABF0 as T1 ON T1.TBBTTA = 'CAT01' AND T1.TBBCTA = IBOCCA AND T1.TBBCLI = 'IT' " +
                          "INNER JOIN IBAOGGF0 ON IBACOG = IBOCES " +
                          "LEFT join IBFREGF0 on IBFOGG = IBOCES and IBFTRE = 'FIS' " +
                          "LEFT JOIN TBBTABF0 AS T2 ON T2.TBBTTA = 'NAZ' AND T2.TBBCTA = IBANAN AND T2.TBBCLI = 'IT' " +
                          "LEFT JOIN TBBTABF0 AS T4 ON T4.TBBTTA = 'IND' AND T4.TBBCTA = IBAIND AND T4.TBBCLI = 'IT' " +
                          "LEFT JOIN TBBTABF0 AS T5 ON T5.TBBTTA = 'NAZ' AND T5.TBBCTA = IBANAZ AND T5.TBBCLI = 'IT' " +
                          $"WHERE IBOOGG = @oggetto AND IBOFLC = ' ' Order By IBOFLC, IBADNA asc";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@oggetto", oggetto);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
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
                                htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["TipoInd"]} {reader["IBADEI"]} {reader["IBACII"]}</font></td>");
                                htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["IBACAP"]} {reader["IBACIT"]}</font></td>");
                                htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{reader["IBAPRV"]}</font></td>");
                                htEredi.AppendLine($"<td width='10%' align='LEFT'><font face='verdana' size='2' color='navy'>{note}</font></td>");
                                htEredi.AppendLine("</tr>");
                            }
                        }
                        else
                        {
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

            return htEredi.ToString();
        }
    }
}
