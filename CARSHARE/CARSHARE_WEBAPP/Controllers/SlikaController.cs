using Microsoft.AspNetCore.Mvc;

namespace CARSHARE_WEBAPP.Controllers
{
    public class SlikaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
