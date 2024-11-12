using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CreazioneListe.Controllers
{
    public class SelezionaSscController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IModuloService _moduloService;
        private readonly IExcelService _excelService;
        private readonly IConfiguration _configuration;

        public SelezionaSscController(IDatabaseService databaseService,
                                      IExcelService excelService,
                                      IConfiguration configuration,
                                      IModuloService moduloService)
        {
            _databaseService = databaseService;
            _excelService = excelService;
            _configuration = configuration;
            _moduloService = moduloService;
        }

        public IActionResult Index()
        {
            var formData = new FormData();
            return View(formData);
        }

        [HttpPost]
        public IActionResult Submit(FormData formData)
        {
            return RedirectToAction("SelezionaSsc", "SelezionaSsc", formData);
        }

        public async Task<IActionResult> SelezionaSsc(FormData formData)
        {
            // Utilizza il servizio per chiamare LeggiModuloTesto
            var moduloTesto = _moduloService.LeggiModuloTesto("someOggetto", "someTipo", "SSC");
            var eredi = _moduloService.LeggiEredi("someOggetto", "someRigo", "SSC");
            // Utilizza moduloTesto e eredi come necessario
            var data = await _databaseService.GetSelectAsync(formData, "SSC");
            return View(data);
        }
        public async Task<IActionResult> TestQuery_view(string[] selectedRows)
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

                if (datiSelezionati.Length < 5)
                {
                    return BadRequest("Informazioni insufficienti per la riga selezionata.");
                }

                int rowIndex = int.Parse(datiSelezionati[4]);
                string formatoSelezionato = Request.Form[$"formato_{rowIndex}"];

                var richiestaExcel = new RichiestaExcel
                {
                    NazCor = datiSelezionati[0],
                    CodCor = datiSelezionati[1],
                    CodAcc = datiSelezionati[2],
                    CodUrg = datiSelezionati[3],
                    Formato = formatoSelezionato
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
                var data = await _databaseService.GetSelectedAsync(richiestaExcel, formData, "SSC");

                // Filtra e rielabora le colonne che vuoi visualizzare
                var dataFiltrata = FiltraColonne(data, richiestaExcel);

                dataTables.Add(dataFiltrata);
            }

            return View("~/Views/Test/VisualizzaDati.cshtml", dataTables);
        }

        public async Task<IActionResult> CreaFile(string[] selectedRows, string[] formato, string tenant = "SSC")
        {
            if (selectedRows == null || selectedRows.Length == 0)
            {
                return BadRequest("Nessuna riga selezionata.");
            }

            var dataTables = new List<DataTable>();
            var richiesteExcel = new List<RichiestaExcel>();

            foreach (var selectedRow in selectedRows)
            {
                var datiSelezionati = selectedRow.Split(',');
                var richiestaExcel = new RichiestaExcel
                {
                    NazCor = datiSelezionati.Length > 0 ? datiSelezionati[0] : null,
                    CodCor = datiSelezionati.Length > 1 ? datiSelezionati[1] : null,
                    CodAcc = datiSelezionati.Length > 2 ? datiSelezionati[2] : null,
                    CodUrg = datiSelezionati.Length > 3 ? datiSelezionati[3] : null,
                    Formato = formato.Length > richiesteExcel.Count ? formato[richiesteExcel.Count] : null // Assegna il formato corretto per ciascuna richiesta
                };

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

                var data = await _databaseService.GetSelectedAsync(richiestaExcel, formData, tenant);
                richiestaExcel.TotRic = data.Rows.Count;
                richiesteExcel.Add(richiestaExcel);

                var dataFiltrata = FiltraColonne(data, richiestaExcel);
                dataTables.Add(dataFiltrata);

                foreach (DataRow row in data.Rows)
                {
                    _moduloService.AggiornaFileCorrispondenti(
                        int.Parse(row["PBAANP"].ToString()),
                        int.Parse(row["PBANUP"].ToString()),
                        richiestaExcel.CodAcc,
                        richiestaExcel.CodUrg,
                        richiestaExcel.NazCor,
                        richiestaExcel.CodCor,
                        "",
                        tenant
                    );
                }
            }

            var files = await _excelService.CreateExcelFilesAsync(dataTables, richiesteExcel, tenant);
            ViewBag.Tenant = tenant;  // Salva tenant nel ViewBag per passarlo alla vista
            return View("~/Views/File/ListaFileSsc.cshtml", files);
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

            // Verifica e aggiungi eventuali colonne opzionali in base al formato
            if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("PBACLI"))
            {
                if (!dataTableFiltrato.Columns.Contains("CLIENTE"))
                {
                    dataTableFiltrato.Columns.Add("CLIENTE", typeof(string));
                }
            }

            if (richiesta.Formato == "EREDI")
            {
                dataTableFiltrato.Columns.Add("PrimoRigo", typeof(string));
            }

            foreach (DataRow row in dataTable.Rows)
            {
                var newRow = dataTableFiltrato.NewRow();

                if (richiesta.CodCor == "8033")
                {
                    newRow["Azienda"] = "SSC";
                }

                // Verifica se le colonne esistono nel DataTable originale prima di accedervi
                if (dataTable.Columns.Contains("PBAANP") && dataTable.Columns.Contains("PBANUP"))
                {
                    newRow["Protocollo"] = row["PBAANP"].ToString() + row["PBANUP"].ToString();
                }
                else
                {
                    newRow["Protocollo"] = "N/A";
                }

                string codiceFiscale = string.Empty;

                // Se PBACFI è vuoto, utilizza PBAPIV
                if (dataTable.Columns.Contains("PBACFI") && !string.IsNullOrWhiteSpace(row["PBACFI"].ToString()))
                {
                    codiceFiscale = row["PBACFI"].ToString();
                }
                else if (dataTable.Columns.Contains("PBAPIV") && !string.IsNullOrWhiteSpace(row["PBAPIV"].ToString()))
                {
                    codiceFiscale = row["PBAPIV"].ToString();
                }

                // Se il codice fiscale ha lunghezza 11, aggiungi un apice all'inizio
/*                if (codiceFiscale.Length == 11)
                {
                    codiceFiscale = "'" + codiceFiscale;
                }*/

                newRow["Codice Fiscale"] = string.IsNullOrEmpty(codiceFiscale) ? "N/A" : codiceFiscale;

                // Popola la colonna CLIENTE solo se richiesto
                if (richiesta.Formato == "CLIENTE" && dataTable.Columns.Contains("PBACLI"))
                {
                    newRow["CLIENTE"] = row["PBACLI"].ToString();
                }

                if (richiesta.CodAcc == "BAN" || richiesta.CodAcc == "CCL")
                {
                    if (!dataTableFiltrato.Columns.Contains("PBADEN"))
                    {
                        dataTableFiltrato.Columns.Add("PBADEN", typeof(string));
                    }
                    newRow["PBADEN"] = row["PBADEN"].ToString();
                }

                if (richiesta.CodAcc == "CMO")
                {
                    if (!dataTableFiltrato.Columns.Contains("PBADEN"))
                    {
                        dataTableFiltrato.Columns.Add("PBADEN", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("PBACIT"))
                    {
                        dataTableFiltrato.Columns.Add("PBACIT", typeof(string));
                    }
                    newRow["PBADEN"] = row["PBADEN"].ToString();
                    newRow["PBACIT"] = row["PBACIT"].ToString();
                }

                if (richiesta.CodAcc == "VED")
                {
                    if (!dataTableFiltrato.Columns.Contains("P.Iva"))
                    {
                        dataTableFiltrato.Columns.Add("P.Iva", typeof(string));
                    }
                    if (!dataTableFiltrato.Columns.Contains("ESITO"))
                    {
                        dataTableFiltrato.Columns.Add("ESITO", typeof(string));
                    }
                    newRow["P.Iva"] = row["LAVCFI"].ToString();
                    newRow["ESITO"] = "";
                }

                if (richiesta.CodAcc == "DP1")
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        string columnName = $"ESITO {i}";
                        if (!dataTableFiltrato.Columns.Contains(columnName))
                        {
                            dataTableFiltrato.Columns.Add(columnName, typeof(string));
                        }
                        newRow[columnName] = "";
                    }
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
                        primoRigo += _moduloService.LeggiModuloTesto(row["PBAOGG"].ToString(), "003", "SSC");
                    }

                    if (dataTable.Columns.Contains("IBACOG"))
                    {
                        var erediHtml = _moduloService.LeggiEredi(row["IBACOG"].ToString(), primoRigo, "SSC");
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

    }
}
