using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace CreazioneListe.Controllers
{
    public class SelezionaTenantBController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IModuloService _moduloService;
        private readonly IExcelService _excelService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IColonneFiltraggioService _colonneFiltraggioService;

        private readonly ILogger<SelezionaTenantBController> _logger;

        public SelezionaTenantBController(IDatabaseService databaseService,
                                      IExcelService excelService,
                                      IConfiguration configuration,
                                      IModuloService moduloService,
                                      IMemoryCache memoryCache,
                                      ILogger<SelezionaTenantBController> logger,
                                      IColonneFiltraggioService colonneFiltraggioService)
        {
            _databaseService = databaseService;
            _excelService = excelService;
            _configuration = configuration;
            _moduloService = moduloService;
            _memoryCache = memoryCache;
            _logger = logger;
            _colonneFiltraggioService = colonneFiltraggioService;
        }

        public IActionResult Index()
        {
            try
            {
                _logger.LogInformation("Accedendo alla pagina Index del controller SelezionaTENANT_B.");
                var formData = new FormData();
                return View(formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'accesso alla pagina Index del controller SelezionaTENANT_B.");
                return StatusCode(500, "Errore durante l'accesso alla pagina. Si prega di riprovare più tardi.");
            }
        }

        [HttpPost]
        public IActionResult Submit(FormData formData)
        {
            try
            {
                _logger.LogInformation("Dati inviati per la selezione TENANT_B.");
                return RedirectToAction("SelezionaTenantB", "SelezionaTenantB", formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il redirect a SelezionaTENANT_B.");
                return StatusCode(500, "Errore durante il redirect. Si prega di riprovare più tardi.");
            }
        }

        public async Task<IActionResult> SelezionaTENANT_B(FormData formData)
        {
            try
            {
                _logger.LogInformation("Esecuzione della selezione TENANT_B per i dati forniti.");
                var data = await _databaseService.GetSelectAsync(formData, "TENANT_B");
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la selezione dei dati TENANT_B.");
                return StatusCode(500, "Errore durante la selezione dei dati. Si prega di riprovare più tardi.");
            }
        }

        public async Task<IActionResult> CreaFile(string[] selectedRows, string[] formato, string[] unisci, string tenant = "TENANT_B")
        {
            if (selectedRows == null || selectedRows.Length == 0)
            {
                _logger.LogWarning("Nessuna riga selezionata per la creazione del file.");
                return BadRequest("Nessuna riga selezionata.");
            }

            try
            {
                var richiesteExcel = new List<RichiestaExcel>();
                var dataTables = new List<DataTable>();
                FormData formData = null;

                var richiesteDaUnire = new Dictionary<string, List<RichiestaExcel>>();
                var dataTablesDaUnire = new Dictionary<string, List<DataTable>>();

                for (int i = 0; i < selectedRows.Length; i++)
                {
                    var datiSelezionati = selectedRows[i].Split(',');

                    var richiestaExcel = new RichiestaExcel
                    {
                        NazCor = datiSelezionati[0],
                        CodCor = datiSelezionati[1],
                        CodAcc = datiSelezionati[2],
                        CodUrg = datiSelezionati[3],
                        Formato = formato.ElementAtOrDefault(i) ?? "B"
                    };

                    formData = new FormData
                    {
                        DaDataAff = DateTime.Now.AddDays(-7).ToString("yyyyMMdd"),
                        DataAff = DateTime.Now.ToString("yyyyMMdd"),
                        NazCor = richiestaExcel.NazCor,
                        CodCor = richiestaExcel.CodCor,
                        CodAcc = richiestaExcel.CodAcc
                    };

                    var data = await _databaseService.GetSelectedAsync(richiestaExcel, formData, tenant);
                    richiestaExcel.TotRic = data.Rows.Count;
                    var dataFiltrata = _colonneFiltraggioService.FiltraColonne(data, richiestaExcel, tenant);

                    /*foreach (DataRow row in data.Rows)
                    {
                        _moduloService.AggiornaFileCorrispondenti(
                            int.Parse(row["AnnoProtocollo"].ToString()),
                            int.Parse(row["NumeroProtocollo"].ToString()),
                            richiestaExcel.CodAcc,
                            richiestaExcel.CodUrg,
                            richiestaExcel.NazCor,
                            richiestaExcel.CodCor,
                            "",
                            tenant
                        );
                    }*/

                    if (unisci.ElementAtOrDefault(i) == "true")
                    {
                        string chiaveUnione = richiestaExcel.CodAcc;
                        if (!richiesteDaUnire.ContainsKey(chiaveUnione))
                        {
                            richiesteDaUnire[chiaveUnione] = new List<RichiestaExcel>();
                            dataTablesDaUnire[chiaveUnione] = new List<DataTable>();
                        }
                        richiesteDaUnire[chiaveUnione].Add(richiestaExcel);
                        dataTablesDaUnire[chiaveUnione].Add(dataFiltrata);
                    }
                    else
                    {
                        richiesteExcel.Add(richiestaExcel);
                        dataTables.Add(dataFiltrata);
                    }
                }

                // Unione dei DataTable con la stessa CodAcc
                foreach (var codAcc in richiesteDaUnire.Keys)
                {
                    var unioneDataTable = dataTablesDaUnire[codAcc].First().Clone(); // Clona la struttura
                    foreach (var dt in dataTablesDaUnire[codAcc])
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            unioneDataTable.ImportRow(row);
                        }
                    }

                    var richiestaUnita = richiesteDaUnire[codAcc].First();
                    richiestaUnita.TotRic = unioneDataTable.Rows.Count;

                    richiesteExcel.Add(richiestaUnita);
                    dataTables.Add(unioneDataTable);
                }

                var files = await _excelService.CreateExcelFilesAsync(dataTables, richiesteExcel, formData, tenant);

                ViewBag.Tenant = tenant;

                return View("~/Views/File/ListaFileTenantB.cshtml", files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dei file Excel per il tenant {Tenant}.", tenant);
                return StatusCode(500, "Errore durante la creazione dei file Excel.");
            }
        }

    }
}
