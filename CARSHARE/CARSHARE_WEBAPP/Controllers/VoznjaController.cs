using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace CARSHARE_WEBAPP.Controllers
{
    public class VoznjaController : Controller
    {
        [HttpGet]
        public IActionResult GetVoznjaMocked() 
        { 
            var voznja = MockDB.GetVoznje(); 

            var voznjaVM = voznja.Select(o => new VoznjaVM 
            { 
                Voziloid = o.Voziloid,
                DatumIVrijemePolaska = o.DatumIVrijemePolaska,
                DatumIVrijemeDolaska = o.DatumIVrijemeDolaska,
                Troskoviid = o.Troskoviid,
                BrojPutnika = o.BrojPutnika,
                Statusvoznjeid = o.Statusvoznjeid,
                Lokacijaid = o.Lokacijaid
            }).ToList();

            return View(voznjaVM);
        }

    }
}
