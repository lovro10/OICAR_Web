using CARSHARE_WEBAPP.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CARSHARE_WEBAPP.ViewModels;
using CARSHARE_WEBAPP.Services;

namespace CARSHARE_WEBAPP.Controllers
{
    public class KorisnikController : Controller
    {
        private readonly KorisnikService _korisnikService;
        private readonly HttpClient _httpClient;
  

        public KorisnikController(KorisnikService korisnikService)
        {
            _korisnikService = korisnikService;
        }

        public async Task<IActionResult> GetKorisnici()
        {
            var korisnici = await _korisnikService.GetKorisniciAsync();
            return View(korisnici);
        }
        [HttpGet]
        public IActionResult GetKorisniciMocked()
        {
            var korisnici = MockDB.GetKorisnici();
         
            var korisniciVM = korisnici.Select(k => new KorisnikVM
            {
                IDKorisnik = k.IDKorisnik,
                Ime = k.Ime,
                Prezime = k.Prezime,
                Email = k.Email,
                Username = k.Username,
                Telefon = k.Telefon,         
            }).ToList();

            return View(korisniciVM);
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


