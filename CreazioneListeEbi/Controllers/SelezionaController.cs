using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CreazioneListeEbi.Controllers
{
    public class SelezionaController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly IExcelService _excelService;

        public SelezionaController(IDatabaseService databaseService, IExcelService excelService)
        {
            _databaseService = databaseService;
            _excelService = excelService;
        }

        public async Task<IActionResult> Seleziona(FormData formData)
        {
            var data = await _databaseService.GetDataAsync(formData);
            return View(data);
        }

        public async Task<IActionResult> Crea(FormData formData)
        {
            var data = await _databaseService.GetDataAsync(formData);
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");
            var filePath = await _excelService.CreateExcelAsync(data, directoryPath);

            return RedirectToAction("ListaFile", "File");
        }
    }
}
