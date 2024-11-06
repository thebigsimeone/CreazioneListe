using CreazioneListe.Interfaces;
using CreazioneListe.Models;
using Microsoft.AspNetCore.Mvc;

namespace CreazioneListe.Controllers
{
    public class CreazioneController : Controller
    {
        public class TestController : Controller
        {
            private readonly IDatabaseService _databaseService;

            public TestController(IDatabaseService databaseService)
            {
                _databaseService = databaseService;
            }

            public async Task<IActionResult> VisualizzaDati(RichiestaExcel richiestaExcel, FormData formData)
            {
                var data = await _databaseService.GetSelectedAsync(richiestaExcel, formData, "EBI");
                return View(data);
            }
        }
    }
}
