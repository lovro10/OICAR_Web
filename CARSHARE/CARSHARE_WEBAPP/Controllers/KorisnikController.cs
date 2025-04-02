using CARSHARE_WEBAPP.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CARSHARE_WEBAPP.ViewModels;

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

            return RedirectToAction("GetKorisnici", existingUser.UlogaID == 1 ? "Korisnik" : "Home");
        }
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Login", "Korisnik");
        }
    }
}

