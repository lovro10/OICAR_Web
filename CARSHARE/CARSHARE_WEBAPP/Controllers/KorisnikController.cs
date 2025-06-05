using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CARSHARE_WEBAPP.ViewModels;
using CARSHARE_WEBAPP.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using CARSHARE_WEBAPP.Security;

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

            var b64hash = PasswordHashProvider.GetHash(loginVM.Password, user.PwdSalt);
            if (b64hash != user.PwdHash)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5194/api/Korisnik/Login");
            var loginPayload = new
            {
                Username = loginVM.UserName,
                Password = loginVM.Password
            };

            var response = await client.PostAsJsonAsync("http://localhost:5194/api/Korisnik/Login", loginPayload);

            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadAsStringAsync();
                var cleanToken = token.Replace("\"", "");

                Response.Cookies.Append("JWToken", cleanToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Append("Username", loginVM.UserName, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Append("UserId", user.IDKorisnik.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Append("Role", user.Uloga.Naziv, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Console.WriteLine("User logged in successfully");
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Neispravno korisničko ime ili lozinka.");
                return View();
            }

        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            Response.Cookies.Delete("JWToken");
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("UserId");
            Response.Cookies.Delete("Role");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var korisnici = await _korisnikService.GetKorisniciAsync();
            var korisnik = korisnici.FirstOrDefault(x => x.IDKorisnik == id);

            if (korisnik == null)
            {
                return NotFound();
            }
          
            return View(korisnik);
        }

        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var korisnici = await _korisnikService.GetKorisniciAsync();
            var korisnik = korisnici.FirstOrDefault(x => x.IDKorisnik == id);

            if (korisnik == null)
            {
                return NotFound();
            }

            var updateVM = new EditKorisnikVM
            {
                IDKorisnik = korisnik.IDKorisnik,
                Ime = korisnik.Ime,
                Prezime = korisnik.Prezime,
                Email = korisnik.Email,
                Telefon = korisnik.Telefon,
                DatumRodjenja = korisnik.DatumRodjenja,
                Username = korisnik.Username
            };

            return View(updateVM);
        }
       
        [HttpPost]
        public async Task<IActionResult> Update(EditKorisnikVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var korisnik = new EditKorisnikVM
                {
                    IDKorisnik = model.IDKorisnik,
                    Ime = model.Ime,
                    Prezime = model.Prezime,
                    Email = model.Email,
                    Telefon = model.Telefon,
                    DatumRodjenja = model.DatumRodjenja,
                    Username = model.Username
                 
                };

                var response = await _korisnikService.UpdateKorisnikAsync(korisnik);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Details", new { id = model.IDKorisnik });
                }

                ModelState.AddModelError("", "Failed to update user. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Images()
        {
            var jwt = Request.Cookies["JWToken"];
            var images = await _korisnikService.GetImagesAsync(jwt);
            return View(images);
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
