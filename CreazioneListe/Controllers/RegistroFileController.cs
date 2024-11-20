using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using CreazioneListe.Services;
using Microsoft.AspNetCore.Mvc;

namespace CreazioneListeEbi.Controllers
{
    public class RegistroFileController : Controller
    {
        private readonly IRegistroFileService _registroFileService;
        private readonly IFileService _fileService;
        private readonly ILogger<RegistroFileController> _logger;

        public RegistroFileController(IRegistroFileService registroFileService, IFileService fileService, ILogger<RegistroFileController> logger)
        {
            _registroFileService = registroFileService;
            _fileService = fileService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var formData = new FormData();
            return View(formData);
        }

        public async Task<IActionResult> RegistroFile(string tenant, string dataAff)
        {
            try
            {
                var registroFiles = await _registroFileService.GetRegistroFilesByDataAsync(dataAff, tenant);

                ViewBag.Tenant = tenant;
                ViewBag.DataAff = dataAff;

                return View("~/Views/RegistroFile/Index.cshtml", registroFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei file dal registro per il tenant {Tenant} e data {DataAff}.", tenant, dataAff);
                return StatusCode(500, "Errore durante il recupero dei file. Si prega di riprovare più tardi.");
            }
        }

        public async Task<IActionResult> ScaricaFile(string tenant, string dataAff, string fileName)
        {
            try
            {
                var file = _fileService.GetFileByDataAff(tenant, dataAff, fileName);
                var memory = new MemoryStream();
                using (var stream = new FileStream(file.FullName, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                _logger.LogInformation("Download del file {FileName} per il tenant {Tenant} e data {DataAff} riuscito.", fileName, tenant, dataAff);
                return File(memory, "application/octet-stream", Path.GetFileName(file.FullName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il download del file {FileName} per il tenant {Tenant} e data {DataAff}.", fileName, tenant, dataAff);
                return StatusCode(500, "Errore durante il download del file. Si prega di riprovare più tardi.");
            }
        }
        public IActionResult ScaricaTuttiFile(string tenant, string dataAff)
        {
            try
            {
                var zipFile = _fileService.CreateZipFileByDataAff(tenant, dataAff);
                var memory = new MemoryStream();
                using (var stream = new FileStream(zipFile.FullName, FileMode.Open))
                {
                    stream.CopyTo(memory);
                }
                memory.Position = 0;
                _logger.LogInformation("Download del file ZIP per il tenant {Tenant} e data {DataAff} riuscito.", tenant, dataAff);
                return File(memory, "application/zip", Path.GetFileName(zipFile.FullName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il download del file ZIP per il tenant {Tenant} e data {DataAff}.", tenant, dataAff);
                return StatusCode(500, "Errore durante il download del file ZIP. Si prega di riprovare più tardi.");
            }
        }

    }
}
