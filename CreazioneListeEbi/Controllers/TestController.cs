using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace CreazioneListe.Controllers
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
            var data = _databaseService.GetSelectedAsync(richiestaExcel, formData, "EBI").Result;
            return View(data);
        }

    }
}
