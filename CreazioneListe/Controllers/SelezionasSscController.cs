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

                var dataTables = new List<DataTable>();
                var richiesteExcel = new List<RichiestaExcel>();
                FormData formData = null;  // Inizializza formData come variabile locale

                foreach (var selectedRow in selectedRows)
                {
                    var datiSelezionati = selectedRow.Split(',');
                    var richiestaExcel = new RichiestaExcel
                    {
                        NazCor = datiSelezionati.Length > 0 ? datiSelezionati[0] : null,
                        CodCor = datiSelezionati.Length > 1 ? datiSelezionati[1] : null,
                        CodAcc = datiSelezionati.Length > 2 ? datiSelezionati[2] : null,
                        CodUrg = datiSelezionati.Length > 3 ? datiSelezionati[3] : null,
                        Formato = formato.Length > richiesteExcel.Count ? formato[richiesteExcel.Count] : null
                    };

                    // Crea e popola formData
                    formData = new FormData
                    {
                        DaDataAff = DateTime.Now.AddDays(-7).ToString("yyyyMMdd"),
                        DataAff = DateTime.Now.ToString("yyyyMMdd"),
                        NazCor = richiestaExcel.NazCor,
                        CodCor = richiestaExcel.CodCor,
                        CodAcc = richiestaExcel.CodAcc
                    };

                    // Ottieni i dati dal database
                    var data = await _databaseService.GetSelectedAsync(richiestaExcel, formData, tenant);
                    richiestaExcel.TotRic = data.Rows.Count;
                    richiesteExcel.Add(richiestaExcel);

                    // Filtra i dati utilizzando il servizio appropriato
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

                // Passa formData al metodo CreateExcelFilesAsync
                var files = await _excelService.CreateExcelFilesAsync(dataTables, richiesteExcel, formData, tenant);
                ViewBag.Tenant = tenant;

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