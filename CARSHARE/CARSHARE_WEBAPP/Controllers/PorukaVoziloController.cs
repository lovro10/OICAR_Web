using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace CARSHARE_WEBAPP.Controllers
{
    public class PorukaVoziloController : Controller
    {
        private readonly HttpClient _httpClient;

        public PorukaVoziloController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5194/api/");
        }

        [HttpGet]
        public async Task<IActionResult> Index(int korisnikVoziloId, int? putnikId, int? vozacId)
        {
            var response = await _httpClient.GetAsync($"Poruka/GetMessagesForVehicle?korisnikVoziloId={korisnikVoziloId}");

            var messages = new List<PorukaVoziloGetVM>();
            if (response.IsSuccessStatusCode)
            {
                messages = await response.Content.ReadFromJsonAsync<List<PorukaVoziloGetVM>>();
            } 

            var porukaVM = new PorukaVoziloSendVM 
            { 
                Korisnikvoziloid = korisnikVoziloId,
                PutnikId = putnikId,
                VozacId = vozacId,
                Messages = messages 
            }; 

            return View(porukaVM);
        } 

        [HttpPost]
        public async Task<IActionResult> SendMessage(PorukaVoziloSendVM porukaVoziloSendVM) 
        { 
            if (string.IsNullOrWhiteSpace(porukaVoziloSendVM.Message))
            { 
                ModelState.AddModelError("", "Message cannot be empty.");
                return RedirectToAction("Index", new
                {
                    korisnikVoziloId = porukaVoziloSendVM.Korisnikvoziloid,
                    putnikId = porukaVoziloSendVM.PutnikId,
                    vozacId = porukaVoziloSendVM.VozacId
                }); 
            } 

            var userIdClaim = User.FindFirst("sub");
            var roleClaim = User.FindFirst("role");

            if (userIdClaim == null || roleClaim == null)
            {
                return Unauthorized();
            }

            int userId = int.Parse(userIdClaim.Value);
            string role = roleClaim.Value.ToUpper();

            int? putnikId = null;
            int? vozacId = null;

            if (role == "DRIVER")
            {
                vozacId = userId;
            }
            else if (role == "PASSENGER")
            {
                putnikId = userId;
            }
            else
            {
                return BadRequest("User role is not valid for sending messages.");
            }

            var sendPayload = new
            {
                korisnikVoziloId = porukaVoziloSendVM.Korisnikvoziloid,
                content = porukaVoziloSendVM.Message,
                putnikId = putnikId, 
                vozacId = vozacId 
            }; 

            var response = await _httpClient.PostAsJsonAsync("Poruka/SendMessageForVehicle", sendPayload); 

            if (!response.IsSuccessStatusCode) 
            { 
                ModelState.AddModelError("", "Failed to send message.");
            } 

            return RedirectToAction("Index", new
            { 
                korisnikVoziloId = porukaVoziloSendVM.Korisnikvoziloid, 
                putnikId = putnikId, 
                vozacId = vozacId 
            }); 
        }
    }
}
