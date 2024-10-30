using CreazioneListeEbi.Interfaces;
using CreazioneListeEbi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CreazioneListeEbi.Controllers
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
                var data = await _databaseService.GetDataTestAsync(richiestaExcel, formData);
                return View(data);
            }
        }
    }
}
