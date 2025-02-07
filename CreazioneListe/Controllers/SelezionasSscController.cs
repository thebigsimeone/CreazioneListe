using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SqlServer.Server;
using System.Data;

namespace CreazioneListe.Controllers
{
    public class SelezionaSscController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IModuloService _moduloService;
        private readonly IExcelService _excelService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IColonneFiltraggioService _colonneFiltraggioService;

        private readonly ILogger<SelezionaSscController> _logger;

        public SelezionaSscController(IDatabaseService databaseService,
                                      IExcelService excelService,
                                      IConfiguration configuration,
                                      IModuloService moduloService,
                                      IMemoryCache memoryCache,
                                      ILogger<SelezionaSscController> logger,
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
                _logger.LogInformation("Accedendo alla pagina Index del controller SelezionaSSC.");
                var formData = new FormData();
                return View(formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'accesso alla pagina Index del controller SelezionaSSC.");
                return StatusCode(500, "Errore durante l'accesso alla pagina. Si prega di riprovare più tardi.");
            }
        }

        [HttpPost]
        public IActionResult Submit(FormData formData)
        {
            try
            {
                _logger.LogInformation("Dati inviati per la selezione SSC.");
                return RedirectToAction("SelezionaSsc", "SelezionaSsc", formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il redirect a SelezionaSSC.");
                return StatusCode(500, "Errore durante il redirect. Si prega di riprovare più tardi.");
            }
        }

        public async Task<IActionResult> SelezionaSSC(FormData formData)
        {
            try
            {
                _logger.LogInformation("Esecuzione della selezione SSC per i dati forniti.");
                var data = await _databaseService.GetSelectAsync(formData, "SSC");
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la selezione dei dati SSC.");
                return StatusCode(500, "Errore durante la selezione dei dati. Si prega di riprovare più tardi.");
            }
        }

        public async Task<IActionResult> CreaFile(string[] selectedRows, string[] formato, string tenant = "SSC")
        {
            if (selectedRows == null || selectedRows.Length == 0)
            {
                _logger.LogWarning("Nessuna riga selezionata per la creazione del file.");
                return BadRequest("Nessuna riga selezionata.");
            }

            try
            {
                _logger.LogInformation("Avvio della creazione dei file Excel per il tenant {Tenant}.", tenant);

                // Crea una chiave per il cache
                var cacheKey = $"CreaFile_{tenant}_{string.Join("_", selectedRows)}";

                ExcelFileCacheData cacheData;

                // Controlla se i dati sono già in cache
                if (_memoryCache.TryGetValue(cacheKey, out cacheData))
                {
                    _logger.LogInformation("Ripristinando dati dalla cache per il tenant {Tenant}.", tenant);
                }
                else
                {
                    var dataTables = new List<DataTable>();
                    var richiesteExcel = new List<RichiestaExcel>();
                    FormData formData = null;

                    foreach (var selectedRow in selectedRows)
                    {
                        var datiSelezionati = selectedRow.Split(',');
                        var richiestaExcel = new RichiestaExcel
                        {
                            NazCor = datiSelezionati.ElementAtOrDefault(0) ?? string.Empty,
                            CodCor = datiSelezionati.ElementAtOrDefault(1) ?? "0",
                            CodAcc = datiSelezionati.ElementAtOrDefault(2) ?? string.Empty,
                            CodUrg = datiSelezionati.ElementAtOrDefault(3) ?? string.Empty,
                            Formato = formato.ElementAtOrDefault(richiesteExcel.Count) ?? string.Empty
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
                        richiesteExcel.Add(richiestaExcel);

                        _logger.LogInformation("Righe selezionate: {SelectedRows}", string.Join(",", selectedRows));
                        _logger.LogInformation("Formati ricevuti: {Formati}", string.Join(",", formato));

                        var dataFiltrata = _colonneFiltraggioService.FiltraColonne(data, richiestaExcel, tenant);
                        dataTables.Add(dataFiltrata);
                        /*foreach (DataRow row in data.Rows)
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
                        }*/
                    }

                    // Crea l'oggetto da mettere in cache
                    cacheData = new ExcelFileCacheData
                    {
                        DataTables = dataTables,
                        RichiesteExcel = richiesteExcel,
                        FormData = formData
                    };

                    // Salva i dati in cache con un timeout di 30 minuti
                    _memoryCache.Set(cacheKey, cacheData, TimeSpan.FromMinutes(30));
                }

                // Utilizza i dati dalla cache
                var files = await _excelService.CreateExcelFilesAsync(cacheData.DataTables, cacheData.RichiesteExcel, cacheData.FormData, tenant);
                ViewBag.Tenant = tenant;

                // Se la creazione dei file è completata con successo, rimuovi la cache
                _memoryCache.Remove(cacheKey);

                _logger.LogInformation("Creazione dei file Excel completata con successo per il tenant {Tenant}.", tenant);
                return View("~/Views/File/ListaFileSsc.cshtml", files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dei file Excel per il tenant {Tenant}.", tenant);
                return StatusCode(500, "Errore durante la creazione dei file Excel. Si prega di riprovare più tardi.");
            }
        }
    }
}