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

namespace CARSHARE_WEBAPP.Controllers
{
    public class KorisnikController : Controller
    {
        private readonly KorisnikService _korisnikService;
        private readonly HttpClient _httpClient;

        public KorisnikController(KorisnikService korisnikService)
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
            return RedirectToAction("GetKorisnici", "Korisnik");
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

            return RedirectToAction("Profile", "Korisnik");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginVM());
        }

        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            using var client = new HttpClient();
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

                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(cleanToken);

                var username = jwt.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var role = jwt.Claims.FirstOrDefault(c => c.Type == "role")?.Value;


                Response.Cookies.Append("JWToken", cleanToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Append("Username", username, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Append("UserId", userId ?? "", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Append("Role", role ?? "", new CookieOptions
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
                return View(loginVM);
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
