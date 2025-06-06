using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace CARSHARE_WEBAPP.Controllers
{
    public class OglasVoziloController : Controller
    {
        private readonly HttpClient _httpClient;

        public OglasVoziloController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5194/api/");
        }

        [HttpGet]
        public async Task<IActionResult> ReserveVehicle(int id)
        {
            var response = await _httpClient.GetAsync($"OglasVozilo/DetaljiOglasaVozila/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var oglas = JsonConvert.DeserializeObject<OglasVoziloVM>(json);

            var reservedDatesResponse = await _httpClient.GetAsync($"OglasVozilo/GetReservedDates/{id}");
            var reservedDatesJson = await reservedDatesResponse.Content.ReadAsStringAsync();
            var reservedDates = JsonConvert.DeserializeObject<List<string>>(reservedDatesJson)
                ?.ConvertAll(dateStr => DateTime.Parse(dateStr)) ?? new List<DateTime>();

            var model = new VehicleReservationVM
            {
                OglasVoziloId = oglas.Idoglasvozilo,
                DozvoljeniPocetak = oglas.DatumPocetkaRezervacije,
                DozvoljeniKraj = oglas.DatumZavrsetkaRezervacije,
                ReservedDates = reservedDates
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReserveVehicle(VehicleReservationVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            model.KorisnikId = userId.Value;

            var normalizedStart = model.DatumPocetkaRezervacije.Date.AddHours(12);
            var normalizedEnd = model.DatumZavrsetkaRezervacije.Date.AddHours(12);

            var newReservation = new
            {
                Korisnikid = model.KorisnikId,
                Oglasvoziloid = model.OglasVoziloId,
                DatumPocetkaRezervacije = normalizedStart,
                DatumZavrsetkaRezervacije = normalizedEnd
            };

            var json = JsonConvert.SerializeObject(newReservation);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("OglasVozilo/CreateReservation", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Reservation was successfully made";
                return RedirectToAction("Index");
            }
            else
            {
                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Rezervacija nije uspjela: {errorText}");
                return View(model);
            }
        }

        private async Task<List<string>> GetCarBrands()
        {
            var carResponse = await _httpClient.GetAsync("CarBrand");
            if (carResponse.IsSuccessStatusCode)
            {
                var carJson = await carResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<string>>(carJson);
            }

            return new List<string>();
        }

        public async Task<IActionResult> IndexUser()
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Account");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var response = await _httpClient.GetAsync($"OglasVozilo/GetAllByUser?userId={userId}");
            if (!response.IsSuccessStatusCode)
                return View(new List<OglasVoziloVM>());

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<OglasVoziloVM>>(json);
            return View(list);
        }

        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("OglasVozilo/GetAll");
            if (!response.IsSuccessStatusCode)
                return View(new List<OglasVoziloVM>());

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<OglasVoziloVM>>(json);
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"OglasVozilo/DetaljiOglasaVoznje/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<OglasVoziloVM>(json);
            return View(dto);
        }

        public async Task<IActionResult> Create(int id)
        {
            Console.WriteLine($"Received Vozilo ID: {id}");

            var response = await _httpClient.GetAsync($"Vozilo/GetVehicleById/{id}");

            Console.WriteLine($"Response status code from GetById: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Vehicle not found.");
                return NotFound("Vehicle not found.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var vozilo = JsonConvert.DeserializeObject<Vozilo>(json);

            if (vozilo == null || vozilo.Vozac == null)
            {
                Console.WriteLine("Vehicle or Driver data not found.");
                return NotFound("Vehicle or Driver data not found.");
            }

            Console.WriteLine($"Retrieved Vehicle: {vozilo.Marka} {vozilo.Model} with Driver: {vozilo.Vozac.Ime} {vozilo.Vozac.Prezime}");

            var model = new OglasVoziloVM
            {
                VoziloId = vozilo.Idvozilo,
                Username = vozilo.Vozac.Username,
                Ime = vozilo.Vozac.Ime,
                Prezime = vozilo.Vozac.Prezime,
                Marka = vozilo.Marka,
                Model = vozilo.Model,
                Registracija = vozilo.Registracija
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(OglasVoziloVM oglasVoziloVM)
        {
            Console.WriteLine("Received data from form:");
            Console.WriteLine($"VoziloId: {oglasVoziloVM.VoziloId}");
            Console.WriteLine($"DatumPocetkaRezervacije: {oglasVoziloVM.DatumPocetkaRezervacije}");
            Console.WriteLine($"DatumZavrsetkaRezervacije: {oglasVoziloVM.DatumZavrsetkaRezervacije}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"ModelState error: {error.ErrorMessage}");
                }
                return View(oglasVoziloVM);
            }

            var normalizedStart = oglasVoziloVM.DatumPocetkaRezervacije.Date.AddHours(12);
            var normalizedEnd = oglasVoziloVM.DatumZavrsetkaRezervacije.Date.AddHours(12);

            var oglas = new Oglasvozilo
            {
                Voziloid = oglasVoziloVM.VoziloId,
                DatumPocetkaRezervacije = normalizedStart,
                DatumZavrsetkaRezervacije = normalizedEnd
            };

            var json = JsonConvert.SerializeObject(oglas);
            Console.WriteLine("Sending POST request to API:");
            Console.WriteLine($"Data: {json}");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("OglasVozilo/KreirajOglasVozilo", content);

            Console.WriteLine($"Response status code from API: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response from API: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Advertisement successfully created.");
                return RedirectToAction("Index");
            }

            Console.WriteLine("Error while creating the advertisement.");
            ModelState.AddModelError("", "Error while creating the advertisement.");
            return View(oglasVoziloVM);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var response = await _httpClient.GetAsync($"OglasVozilo/GetVehicleById/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var oglasVoziloVM = JsonConvert.DeserializeObject<OglasVoziloVM>(json);
            return View(oglasVoziloVM);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, OglasVoziloVM oglasVoziloVM)
        {
            if (!ModelState.IsValid)
                return View(oglasVoziloVM);

            var json = JsonConvert.SerializeObject(oglasVoziloVM);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"OglasVozilo/AzurirajOglasVozilo/{id}", content);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            ModelState.AddModelError("", "Greška pri ažuriranju oglasa.");
            return View(oglasVoziloVM);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Korisnik");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _httpClient.GetAsync($"OglasVozilo/GetOglasVoziloById/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<OglasVoziloVM>(json);
            return View(dto);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var jwtToken = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(jwtToken))
                return RedirectToAction("Login", "Korisnik");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _httpClient.DeleteAsync($"ObrisiOglasVozilo/{id}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Failed to delete vehicle.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("IndexUser");
        }
    }
}
