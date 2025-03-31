using CARSHARE_WEBAPP.Models;
using Microsoft.AspNetCore.Mvc;

namespace CARSHARE_WEBAPP.Controllers
{
    public class KorisnikController : Controller
    {
        [HttpGet]
        public IActionResult GetKorisnici()
        {
            var korisnici = MockDB.GetKorisnici();
            return View(korisnici);
        }
        
        public IActionResult Login()
        {
            return View();
        }
    }
}
