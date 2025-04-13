using CARSHARE_WEBAPP.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CARSHARE_WEBAPP.ViewModels;
using CARSHARE_WEBAPP.Services;
using System.Security.Cryptography;
using System.Text;

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
        public IActionResult Login()
        {
            return View(new LoginVM());
        }

        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            var korisnici = await _korisnikService.GetKorisniciAsync();

     
            var user = korisnici.FirstOrDefault(x => x.Username == loginVM.UserName);

            if (user == null)
            {
                ModelState.AddModelError("", "Incorrect username or password");
                return View(loginVM);
            }

           
            byte[] salt = Convert.FromBase64String(user.PwdSalt);
            using (var hmac = new HMACSHA512(salt))
            {
                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginVM.Password));
                string computedHashString = Convert.ToBase64String(computedHash);

                if (computedHashString != user.PwdHash)
                {
                    ModelState.AddModelError("", "Incorrect username or password");
                    return View(loginVM);
                }
            }

            var role = user.Uloga?.Naziv ?? "USER";
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.NameIdentifier, user.IDKorisnik.ToString()),
        new Claim(ClaimTypes.Role, role)
    };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);


            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("GetKorisnici");
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Korisnik");
        }

        [HttpGet] 
        public async Task<IActionResult> Details(int id)
        {
            var korisnici = await _korisnikService.GetKorisniciAsync();
            var korisnik = korisnici.FirstOrDefault(x => x.IDKorisnik == id);

            if(korisnik == null)
            {
                return NotFound();
            }
            return View(korisnik);
        }

    }
}

//public IActionResult LoginMocked(LoginVM loginVM)
//{
//    var existingUser = MockDB.GetKorisnici().FirstOrDefault(x => x.Username == loginVM.UserName && x.PwdHash == loginVM.Password);

//    if (existingUser == null)
//    {
//        ModelState.AddModelError("", "Incorrect username or password");
//        return View();
//    }

//    var userRole = MockDB.GetUlogas().FirstOrDefault(r => r.IDUloga == existingUser.UlogaID)?.Naziv ?? "USER";
//    var claims = new List<Claim>
//        {
//            new Claim(ClaimTypes.Name, loginVM.UserName),
//            new Claim(ClaimTypes.Role, userRole)
//        };

//    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

//    HttpContext.SignInAsync(
//        CookieAuthenticationDefaults.AuthenticationScheme,
//        new ClaimsPrincipal(claimsIdentity)).Wait();

//    return RedirectToAction("GetKorisniciMocked", existingUser.UlogaID == 1 ? "Korisnik" : "Home");
//}

//[HttpGet]
//public IActionResult GetKorisniciMocked()
//{
//    var korisnici = MockDB.GetKorisnici();

//    var korisniciVM = korisnici.Select(k => new KorisnikVM
//    {
//        IDKorisnik = k.IDKorisnik,
//        Ime = k.Ime,
//        Prezime = k.Prezime,
//        Email = k.Email,
//        Username = k.Username,
//        Telefon = k.Telefon,
//    }).ToList();

//    return View(korisniciVM);
//}
