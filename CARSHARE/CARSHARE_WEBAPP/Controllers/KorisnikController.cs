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
using System.IdentityModel.Tokens.Jwt;
using CARSHARE_WEBAPP.Services.Interfaces;

namespace CARSHARE_WEBAPP.Controllers
{
    public class KorisnikController : Controller
    {
        private readonly IKorisnikService _korisnikService;
        private readonly HttpClient _httpClient;

        public KorisnikController(IKorisnikService korisnikService)
        {
            _korisnikService = korisnikService;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5194/api/Korisnik/")
            };
        }

        public async Task<IActionResult> GetKorisnici()
        {
            var korisnici = await _korisnikService.GetKorisniciAsync();
            return View(korisnici);
        }

        public async Task<IActionResult> GetKorisniciForClear()
        { 
            var response = await _httpClient.GetAsync("GetAllRequestClear"); 
            if (!response.IsSuccessStatusCode) 
                return View(new List<KorisnikVM>());

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<KorisnikVM>>(json); 

            return View(list); 
        } 

        [HttpGet]
        public async Task<IActionResult> ConfirmationPageDetails(int id)
        {
            var response = await _httpClient.GetAsync($"Details?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to load user details.");
                return View();
            }

            var jsonData = await response.Content.ReadAsStringAsync();

            var korisnikVM = JsonConvert.DeserializeObject<KorisnikVM>(jsonData);

            return View(korisnikVM);
        }

        [HttpPost]
        public async Task<IActionResult> ClearUserData(int id)
        {
            var response = await _httpClient.PutAsync($"Clear/{id}", null);

            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                TempData["Message"] = message;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"User with ID {id} was not found.";
            }
            else
            {
                TempData["Error"] = $"Failed to clear user data with ID {id}.";
            }

            var korisnici = await _korisnikService.GetKorisniciAsync();
            return RedirectToAction("GetKorisniciForClear", "Korisnik");
        }

        [HttpGet]
        public async Task<IActionResult> ClearDataRequestPage(int id)
        { 
            var korisnici = await _korisnikService.GetKorisniciAsync();
            var korisnik = korisnici.FirstOrDefault(x => x.IDKorisnik == id);

            if (korisnik == null)
            {
                return NotFound();
            }

            var clearVM = new KorisnikVM 
            {  
                IDKorisnik = korisnik.IDKorisnik,
                Username = korisnik.Username, 
                Ime = korisnik.Ime, 
                Prezime = korisnik.Prezime,
                Email = korisnik.Email,
                Telefon = korisnik.Telefon, 
                DatumRodjenja = korisnik.DatumRodjenja, 
                UlogaNaziv = korisnik.Uloga?.Naziv  
            }; 

            return View(clearVM);
        } 

        [HttpPost] 
        public async Task<IActionResult> ClearDataRequestPage(KorisnikVM model) 
        { 
            var response = await _httpClient.PutAsync($"RequestClearInfo?id={model.IDKorisnik}", null); 

            if (response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                TempData["Message"] = message;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                TempData["Error"] = $"User with ID {model.IDKorisnik} was not found.";
            } 
            else
            { 
                TempData["Error"] = $"Failed to clear user data with ID {model.IDKorisnik}."; 
            } 

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            var response = await _korisnikService.LoginAsync(loginVM);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt");


                Response.Cookies.Append(
                    "X-Login-Failed",
                    "1",
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                    });

                return View(loginVM);
            }

            var raw = (await response.Content.ReadAsStringAsync()).Trim('"');
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);

            string Claim(string t) => jwt.Claims.First(c => c.Type == t).Value;
            var username = Claim("unique_name");
            var userId = Claim("sub");
            var role = Claim("role");

            void Set(string name, string val) =>
                Response.Cookies.Append(name, val, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

            Set("JWToken", raw);
            Set("Username", username);
            Set("UserId", userId);
            Set("Role", role);

            return RedirectToAction("Index", "Home");
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

        public async Task<IActionResult> Details(int id)
        {
            var response = await _httpClient.GetAsync($"Details?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to load user details.");
                return View();
            }

            var jsonData = await response.Content.ReadAsStringAsync();

            var korisnikVM = JsonConvert.DeserializeObject<KorisnikVM>(jsonData);

            return View(korisnikVM);
        }

        public async Task<IActionResult> Profile(int id)
        { 
            var response = await _httpClient.GetAsync($"Profile?id={id}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to load user details.");
                return View();
            }

            var jsonData = await response.Content.ReadAsStringAsync();

            var korisnikVM = JsonConvert.DeserializeObject<KorisnikVM>(jsonData);

            return View(korisnikVM);
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
                Telefon = korisnik.Telefon 
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
                    Telefon = model.Telefon 
                };

                var response = await _korisnikService.UpdateKorisnikAsync(korisnik);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Profile", new { id = model.IDKorisnik });
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


