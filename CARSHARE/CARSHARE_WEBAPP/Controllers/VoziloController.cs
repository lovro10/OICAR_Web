using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace CARSHARE_WEBAPP.Controllers
{
    public class VoziloController : Controller
    {
        private readonly HttpClient _client;

        public VoziloController()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
        }

        public async Task<IActionResult> Index()
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Korisnik");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Korisnik");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _client.GetAsync($"Vozilo/GetVehicleByUser?userId={userId}");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Unable to load vehicles.";
                return View(new List<VoziloVM>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var vozila = JsonConvert.DeserializeObject<List<VoziloVM>>(json);

            return View(vozila);
        }

        private async Task<List<string>> GetCarsAsync()
        { 
            var carResponse = await _client.GetAsync("CarBrand");
            if (carResponse.IsSuccessStatusCode)
            { 
                var carJson = await carResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<string>>(carJson);
            }

            return new List<string>();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Cars = await GetCarsAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(VoziloVM voziloVm)
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (voziloVm.FrontImage == null || voziloVm.BackImage == null)
            {
                ViewBag.Error = "Both images are required.";
                return View(voziloVm);
            }

            string frontBase64 = null!;
            string backBase64 = null!;

            using (var ms = new MemoryStream())
            {
                await voziloVm.FrontImage.CopyToAsync(ms);
                frontBase64 = Convert.ToBase64String(ms.ToArray());
            }

            using (var ms = new MemoryStream())
            {
                await voziloVm.BackImage.CopyToAsync(ms);
                backBase64 = Convert.ToBase64String(ms.ToArray());
            }

            var vozilo = new
            {
                Naziv = voziloVm.Naziv,
                Marka = voziloVm.Marka,
                Model = voziloVm.Model,
                Registracija = voziloVm.Registracija,
                VozacId = userId.Value,
                FrontImageBase64 = frontBase64,
                BackImageBase64 = backBase64,
                FrontImageName = "Prednja prometna " + voziloVm.Registracija,
                BackImageName = "Zadnja prometna " + voziloVm.Registracija
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            var json = JsonConvert.SerializeObject(vozilo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("Vozilo/CreateVehicle", content);

            if (response.IsSuccessStatusCode) 
                return RedirectToAction("Index");

            ViewBag.Error = "Failed to create vehicle.";
            return View(voziloVm); 
        } 

        [HttpGet]
        public async Task<IActionResult> DetailsAdmin()
        { 
            var response = await _client.GetAsync("Vozilo/GetVehicles");

            if (!response.IsSuccessStatusCode) 
            { 
                ModelState.AddModelError("", "Failed to load vehicles.");
                return View(new List<VoziloVM>());
            } 

            var jsonData = await response.Content.ReadAsStringAsync();

            Console.WriteLine("=== JSON received from API ===");
            Console.WriteLine(jsonData);
            Console.WriteLine("==============================");

            var vozila = JsonConvert.DeserializeObject<List<VoziloVM>>(jsonData)
                            ?.OrderByDescending(v => v.Idvozilo) 
                            .ToList(); 

            return View(vozila); 
        } 

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        { 
            var response = await _client.GetAsync($"Vozilo/Details?id={id}");

            if (!response.IsSuccessStatusCode)
            { 
                ViewBag.Error = "Failed to load vehicle details.";
                return View(); 
            }

            var json = await response.Content.ReadAsStringAsync();
            var vozilo = JsonConvert.DeserializeObject<VoziloVM>(json);

            if (vozilo == null)
            {
                ViewBag.Error = "Vehicle details not found.";
                return View();
            }

            return View(vozilo);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Korisnik");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _client.GetAsync($"Vozilo/GetVehicleById/{id}");
            if (!response.IsSuccessStatusCode) 
                return NotFound(); 

            var json = await response.Content.ReadAsStringAsync();
            var vozilo = JsonConvert.DeserializeObject<VoziloVM>(json);
            return View(vozilo); 
        } 

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        { 
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Korisnik");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _client.DeleteAsync($"Vozilo/DeleteVehicle/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Failed to delete vehicle.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
    }
}
