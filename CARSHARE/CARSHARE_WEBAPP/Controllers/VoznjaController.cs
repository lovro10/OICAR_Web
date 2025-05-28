using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CARSHARE_WEBAPP.Controllers
{
    public class VoznjaController : Controller
    {
        private readonly VoznjaService _voznjaService;

        public VoznjaController(VoznjaService voznjaService)
        {
            _voznjaService = voznjaService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // osiguraj JWT
            var jwt = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwt))
                return RedirectToAction("Login", "Korisnik");

            var voznje = await _voznjaService.GetVoznjeAsync(jwt);
            return View(voznje);
        }
    }
}
