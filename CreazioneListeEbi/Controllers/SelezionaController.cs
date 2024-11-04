using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;

namespace CreazioneListeEbi.Controllers
{
    public class SelezionaController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IExcelService _excelService;
        private readonly IConfiguration _configuration;

        public SelezionaController(IDatabaseService databaseService, IExcelService excelService, IConfiguration configuration)
        {
            _databaseService = databaseService;
            _excelService = excelService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Seleziona(FormData formData)
        {
            // Ottiene i dati dal database in base al form di input fornito dall'utente.
            var data = await _databaseService.GetDataAsync(formData);
            return View(data);
        }

        public async Task<IActionResult> Crea(FormData formData)
        {
            // Ottiene i dati dal database in base al form di input
            var data = await _databaseService.GetDataAsync(formData);

            // Creiamo un oggetto RichiestaExcel basato sui dati di input forniti e sui dati recuperati
            var richiesta = new RichiestaExcel
            {
                NazCor = formData.NazCor,
                CodCor = formData.CodCor,
                CodAcc = formData.CodAcc,
                CodUrg = data.Rows.Count > 0 ? data.Rows[0]["PBSURG"].ToString() : null,
                Formato = formData.CodAcc,  // Questo è un esempio; cambia a seconda della logica specifica
                TotRic = data.Rows.Count
            };

            // Creiamo il file Excel utilizzando il servizio di creazione Excel
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");
            var filePath = await _excelService.CreateExcelFileAsync(richiesta);

            return RedirectToAction("ListaFile", "File");
        }
        public async Task<IActionResult> TestQuery(string[] selectedRows, string[] formato)
        {
            if (selectedRows == null || selectedRows.Length == 0)
            {
                return BadRequest("Nessuna riga selezionata.");
            }

            var dataTables = new List<DataTable>();

            foreach (var selectedRow in selectedRows)
            {
                // Parsing dei dati selezionati
                var datiSelezionati = selectedRow.Split(',');
                var richiestaExcel = new RichiestaExcel
                {
                    NazCor = datiSelezionati.Length > 0 ? datiSelezionati[0] : null,
                    CodCor = datiSelezionati.Length > 1 ? datiSelezionati[1] : null,
                    CodAcc = datiSelezionati.Length > 2 ? datiSelezionati[2] : null,
                    CodUrg = datiSelezionati.Length > 3 ? datiSelezionati[3] : null,
                    Formato = formato.Length > 0 ? formato[0] : null
                };

                // Assegna valori costanti per DaDataAff e DataAff
                var daDataAff = DateTime.Now.AddDays(-7).ToString("yyyyMMdd");
                var dataAff = DateTime.Now.ToString("yyyyMMdd");

                var formData = new FormData
                {
                    DaDataAff = daDataAff,
                    DataAff = dataAff,
                    NazCor = richiestaExcel.NazCor,
                    CodCor = richiestaExcel.CodCor,
                    CodAcc = richiestaExcel.CodAcc
                };

                // Recupera tutti i dati dal database
                var data = await _databaseService.GetDataTestAsync(richiestaExcel, formData);

                // Filtra e rielabora le colonne che vuoi visualizzare
                var dataFiltrata = FiltraColonne(data, richiestaExcel);

                dataTables.Add(dataFiltrata);
            }

            return View("~/Views/Test/VisualizzaDati.cshtml", dataTables);
        }

        // Metodo per filtrare e rielaborare le colonne del DataTable con le condizioni richieste
        private DataTable FiltraColonne(DataTable dataTable, RichiestaExcel richiesta)
        {
            var dataTableFiltrato = new DataTable();

            if (richiesta.CodCor == "8033")
            {
                dataTableFiltrato.Columns.Add("Azienda", typeof(string));
            }

            // Aggiungi le colonne principali desiderate
            dataTableFiltrato.Columns.Add("Protocollo", typeof(string));
            dataTableFiltrato.Columns.Add("Codice Fiscale", typeof(string));

            // Verifica e aggiungi eventuali colonne opzionali
            if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("PBACLI"))
            {
                dataTableFiltrato.Columns.Add("PBACLI", typeof(string));
            }

            if (richiesta.Formato == "EREDI")
            {
                dataTableFiltrato.Columns.Add("PrimoRigo", typeof(string));
            }

            // Aggiungi le colonne richieste dai nuovi IF
            if (richiesta.CodAcc == "BAN" || richiesta.CodAcc == "CCL")
            {
                dataTableFiltrato.Columns.Add("PBADEN", typeof(string));
            }

            if (richiesta.CodAcc == "CMO")
            {
                dataTableFiltrato.Columns.Add("PBADEN", typeof(string));
                dataTableFiltrato.Columns.Add("PBACIT", typeof(string));
            }

            if (richiesta.CodAcc == "VED")
            {
                dataTableFiltrato.Columns.Add("P.Iva", typeof(string));
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var newRow = dataTableFiltrato.NewRow();
                if (richiesta.CodCor == "8033")
                {
                    newRow["Azienda"] = "EBI";
                }

                // Verifica se le colonne esistono nel DataTable originale prima di accedervi
                if (dataTable.Columns.Contains("PBAANP") && dataTable.Columns.Contains("PBANUP"))
                {
                    newRow["Protocollo"] = row["PBAANP"].ToString() + row["PBANUP"].ToString();
                }
                else
                {
                    newRow["Protocollo"] = "N/A"; // Valore di default se la colonna non esiste
                }

                if (dataTable.Columns.Contains("PBACFI"))
                {
                    newRow["Codice Fiscale"] = row["PBACFI"].ToString();
                }
                else
                {
                    newRow["Codice Fiscale"] = "N/A"; // Valore di default se la colonna non esiste
                }

                if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("PBACLI") && row["PBACLI"] != DBNull.Value)
                {
                    if (!dataTableFiltrato.Columns.Contains("PBACLI"))
                    {
                        dataTableFiltrato.Columns.Add("PBACLI", typeof(string));
                    }
                    newRow["PBACLI"] = row["PBACLI"].ToString();
                }

                if (richiesta.CodAcc == "BAN" || richiesta.CodAcc == "CCL")
                {
                    newRow["PBADEN"] = row["PBADEN"].ToString();
                }

                if (richiesta.CodAcc == "CMO")
                {
                    newRow["PBADEN"] = row["PBADEN"].ToString();
                    newRow["PBACIT"] = row["PBACIT"].ToString();
                }

                if (richiesta.CodAcc == "VED")
                {
                    newRow["P.Iva"] = row["LAVCFI"].ToString();
                }

                if (richiesta.Formato == "EREDI")
                {
                    var primoRigo = string.Empty;

                    if (dataTable.Columns.Contains("IBARS1") && dataTable.Columns.Contains("IBARS2"))
                    {
                        primoRigo += $"{richiesta.CodAcc} {row["IBARS1"]} {row["IBARS2"]} ";
                    }

                    if (dataTable.Columns.Contains("TipoIND") && dataTable.Columns.Contains("IBADEI") && dataTable.Columns.Contains("IBACII"))
                    {
                        primoRigo += $"{row["TipoIND"]} {row["IBADEI"]} {row["IBACII"]} ";
                    }

                    if (dataTable.Columns.Contains("IBACAP") && dataTable.Columns.Contains("IBACIT"))
                    {
                        primoRigo += $"{row["IBACAP"]} {row["IBACIT"]} ";
                    }

                    if (dataTable.Columns.Contains("IBAPRV"))
                    {
                        primoRigo += $"{row["IBAPRV"]} ";
                    }

                    if (dataTable.Columns.Contains("PBAOGG"))
                    {
                        primoRigo += LeggiModuloTesto(row["PBAOGG"].ToString(), "003");
                    }

                    if (dataTable.Columns.Contains("IBACOG"))
                    {
                        var erediHtml = LeggiEredi(row["IBACOG"].ToString(), primoRigo);
                        newRow["PrimoRigo"] = erediHtml;
                    }
                    else
                    {
                        newRow["PrimoRigo"] = primoRigo;
                    }
                }

                dataTableFiltrato.Rows.Add(newRow);
            }

            return dataTableFiltrato;
        }

        private string LeggiModuloTesto(string cOggetto, string cTipo)
        {
            var altre = string.Empty;
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sql = $"SELECT * FROM IC6DRSF0 WHERE IC6OGG = {cOggetto} AND IC6TMO = '{cTipo}' ORDER BY IC6DAT DESC";
                using (var command = new SqlCommand(sql, connection))
                {
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
        private string LeggiEredi(string oggetto, string primoRigo)
        {
            var htEredi = new StringBuilder();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
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
                          $"WHERE IBOOGG = {oggetto} AND IBOFLC = ' ' Order By IBOFLC, IBADNA asc";

                using (var command = new SqlCommand(sql, connection))
                {
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
