using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;

namespace CARSHARE_WEBAPP.Controllers
{
    public class PorukaVoznjaController : Controller
    {
        private readonly HttpClient _httpClient;

        public PorukaVoznjaController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5194/api/");
        }

        [HttpGet]
        public async Task<IActionResult> Index(int korisnikVoznjaId, int? putnikId, int? vozacId)
        {
            var response = await _httpClient.GetAsync($"Poruka/GetMessagesForRide?korisnikVoznjaId={korisnikVoznjaId}");

            var messages = new List<PorukaGetVM>(); 
            if (response.IsSuccessStatusCode) 
            { 
                messages = await response.Content.ReadFromJsonAsync<List<PorukaGetVM>>();
            } 

            var vm = new PorukaVM 
            {
                Korisnikvoznjaid = korisnikVoznjaId,
                PutnikId = putnikId,
                VozacId = vozacId,
                Messages = messages! 
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(PorukaVM porukaVM) 
        { 
            if (string.IsNullOrWhiteSpace(porukaVM.Message))
            {
                ModelState.AddModelError("", "Message cannot be empty.");
                return RedirectToAction("Index", new
                {
                    korisnikVoznjaId = porukaVM.Korisnikvoznjaid,
                    putnikId = porukaVM.PutnikId,
                    vozacId = porukaVM.VozacId
                });
            }

            var sendPayload = new
            {
                korisnikVoznjaId = porukaVM.Korisnikvoznjaid,
                content = porukaVM.Message,
                putnikId = porukaVM.PutnikId,
                vozacId = porukaVM.VozacId
            };

            var response = await _httpClient.PostAsJsonAsync("Poruka/SendMessageForRide", sendPayload);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to send message.");
            }

            return RedirectToAction("Index", new
            {
                korisnikVoznjaId = porukaVM.Korisnikvoznjaid,
                putnikId = porukaVM.PutnikId,
                vozacId = porukaVM.VozacId
            });
        }
    }
}
