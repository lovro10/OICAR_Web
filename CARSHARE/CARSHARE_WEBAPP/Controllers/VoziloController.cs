using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CARSHARE_WEBAPP.Controllers
{
    public class VoziloController : Controller
    {
        private readonly VoziloService _voziloService;  
        private readonly HttpClient _httpClient; 


        public VoziloController(VoziloService voziloService)
        { 
            _voziloService = voziloService; 
        } 

        public async Task<IActionResult> GetVozila() 
        { 
            var vozila = await _voziloService.GetVozilaAsync(); 
            return View(vozila);  
        } 

        [HttpGet] 
        public IActionResult GetVozilaMocked() 
        { 
            var vozila = MockDB.GetVozila();  

            var vozilaVM = vozila.Select(v => new VoziloVM 
            { 
                IDVozilo = v.Idvozilo, 
                Marka = v.Marka, 
                Model = v.Model, 
                Registracija = v.Registracija 
            }).ToList(); 

            return View(vozilaVM);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVM());
        }

        public IActionResult Login(LoginVM loginVM)
        {
            var existingUser = MockDB.GetKorisnici().FirstOrDefault(x => x.Username == loginVM.UserName && x.PwdHash == loginVM.Password);

            if (existingUser == null)
            {
                ModelState.AddModelError("", "Incorrect username or password");
                return View();
            }

            var userRole = MockDB.GetUlogas().FirstOrDefault(r => r.IDUloga == existingUser.UlogaID)?.Naziv ?? "USER";
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, loginVM.UserName),
            new Claim(ClaimTypes.Role, userRole)
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)).Wait();

            return RedirectToAction("GetKorisniciMocked", existingUser.UlogaID == 1 ? "Korisnik" : "Home");
        }
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Login", "Korisnik");
        }

    }
}
