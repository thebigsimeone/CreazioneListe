using Microsoft.AspNetCore.Mvc;

namespace CreazioneListeEbi.Controllers
{
    public class CreazioneController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
