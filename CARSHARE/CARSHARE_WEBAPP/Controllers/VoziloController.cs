using CARSHARE_WEBAPP.Models;
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
                BaseAddress = new Uri("http://localhost:5194/api/Vozilo/")
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

            var response = await _client.GetAsync($"GetVehicleByUser?userId={userId}");

            if (!response.IsSuccessStatusCode) 
            { 
                ViewBag.Error = "Unable to load vehicles.";
                return View(new List<VoziloVM>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var vozila = JsonConvert.DeserializeObject<List<VoziloVM>>(json);

            return View(vozila);
        }

        [HttpGet]
        public IActionResult Create()
        {
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
                FrontImageName = "Front " + voziloVm.Registracija,
                BackImageName = "Back " + voziloVm.Registracija
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
            var json = JsonConvert.SerializeObject(vozilo);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("KreirajVozilo", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            ViewBag.Error = "Failed to create vehicle.";
            return View(voziloVm);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Korisnik");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _client.GetAsync($"GetVehicleById/{id}");
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

            var response = await _client.DeleteAsync($"DeleteVehicle/{id}"); 
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Failed to delete vehicle.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }
    }
}
