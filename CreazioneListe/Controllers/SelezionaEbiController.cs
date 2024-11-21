using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SqlServer.Server;
using System.Data;

namespace CreazioneListe.Controllers
{
    public class SelezionaEbiController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IModuloService _moduloService;
        private readonly IExcelService _excelService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;
        private readonly IColonneFiltraggioService _colonneFiltraggioService;

        private readonly ILogger<SelezionaEbiController> _logger;

        public SelezionaEbiController(IDatabaseService databaseService,
                                      IExcelService excelService,
                                      IConfiguration configuration,
                                      IModuloService moduloService,
                                      IMemoryCache memoryCache,
                                      ILogger<SelezionaEbiController> logger,
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
                _logger.LogInformation("Accedendo alla pagina Index del controller SelezionaEbi.");
                var formData = new FormData();
                return View(formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'accesso alla pagina Index del controller SelezionaEbi.");
                return StatusCode(500, "Errore durante l'accesso alla pagina. Si prega di riprovare più tardi.");
            }
        }

        [HttpPost]
        public IActionResult Submit(FormData formData)
        {
            try
            {
                _logger.LogInformation("Dati inviati per la selezione EBI.");
                return RedirectToAction("SelezionaEbi", "SelezionaEbi", formData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il redirect a SelezionaEbi.");
                return StatusCode(500, "Errore durante il redirect. Si prega di riprovare più tardi.");
            }
        }

        public async Task<IActionResult> SelezionaEbi(FormData formData)
        {
            try
            {
                _logger.LogInformation("Esecuzione della selezione EBI per i dati forniti.");
                var data = await _databaseService.GetSelectAsync(formData, "EBI");
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la selezione dei dati EBI.");
                return StatusCode(500, "Errore durante la selezione dei dati. Si prega di riprovare più tardi.");
            }
        }
        public async Task<IActionResult> CreaFile(string[] selectedRows, string[] formato, string tenant = "EBI")
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
                FormData formData = null; // Dichiarazione della variabile formData al di fuori del ciclo

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

                    // Assegna un nuovo oggetto a formData per ogni riga selezionata
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

                // Utilizza formData (che ora è stato inizializzato con l'ultima riga processata)
                var files = await _excelService.CreateExcelFilesAsync(dataTables, richiesteExcel, formData, tenant);
                ViewBag.Tenant = tenant;

                _logger.LogInformation("Creazione dei file Excel completata con successo per il tenant {Tenant}.", tenant);
                return View("~/Views/File/ListaFileEbi.cshtml", files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione dei file Excel per il tenant {Tenant}.", tenant);
                return StatusCode(500, "Errore durante la creazione dei file Excel. Si prega di riprovare più tardi.");
            }
        }
    }
}