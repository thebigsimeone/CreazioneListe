using CreazioneListeEbi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CreazioneListeEbi.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var formData = new FormData();
            return View(formData);
        }

        [HttpPost]
        public IActionResult Submit(FormData formData)
        {
            return RedirectToAction("Seleziona", "Seleziona", formData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
