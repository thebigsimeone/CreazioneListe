using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CreazioneListeEbi.Controllers
{
    public class TestController : Controller
    {
        private readonly IDatabaseService _databaseService;

        public TestController(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public IActionResult VisualizzaDati(RichiestaExcel richiestaExcel, FormData formData)
        {
            // Ottiene i dati e restituisce la vista per la visualizzazione dei dati.
            var data = _databaseService.GetDataTestAsync(richiestaExcel, formData).Result;
            return View(data);
        }

    }
}
