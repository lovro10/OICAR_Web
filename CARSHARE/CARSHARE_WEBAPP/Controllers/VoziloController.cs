using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Net;


namespace CARSHARE_WEBAPP.Controllers
{
    public class VoziloController : Controller
    {
        private readonly ILogger<VoziloController> _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public VoziloController(IHttpContextAccessor httpContextAccessor, ILogger<VoziloController> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

        }

        public async Task<IActionResult> Index()
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Index", "Home");

            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5194/api/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("Vozilo/GetAll");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return ShowError(body, response.StatusCode);
            }


            var json = await response.Content.ReadAsStringAsync();
            var vozila = JsonSerializer.Deserialize<List<VoziloVM>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(vozila);
        }


        public IActionResult Create() => View(new Vozilo());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vozilo vm)
        {

            if (!ModelState.IsValid) return View(vm);

            var dto = new
            {
                Marka = vm.Marka,
                Model = vm.Model,
                Registracija = vm.Registracija,
                Prometna = await FileToBase64Async(vm.PrometnaFile)
            };

            using var client = CreateClient();
            var resp = await client.PostAsJsonAsync("Vozilo/KreirajVozilo", dto);

            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Greška pri spremanju vozila.");
                return View(vm);
            }
            return RedirectToAction(nameof(Index));
        }

        // ======== EDIT ========
        public async Task<IActionResult> Edit(int id)
        {
            using var client = CreateClient();
            var resp = await client.GetAsync($"Vozilo/Details/{id}");
            if (!resp.IsSuccessStatusCode)
                return ShowError(await resp.Content.ReadAsStringAsync(), resp.StatusCode);

            var json = await resp.Content.ReadAsStringAsync();
            var vm = JsonSerializer.Deserialize<VoziloVM>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (vm == null)
                return NotFound();

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VoziloVM vm)
        {
            if (id != vm.IDVozilo)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(vm);

            var dto = new
            {
                Marka = vm.Marka,
                Model = vm.Model,
                Registracija = vm.Registracija,
                Prometna = await FileToBase64Async(vm.PrometnaFile)
            };

            using var client = CreateClient();
            var resp = await client.PutAsJsonAsync($"Vozilo/UpdateVozilo/{id}", dto);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"API greška ({(int)resp.StatusCode}): {body}");
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }


        // ======== DELETE ========
        public async Task<IActionResult> Delete(int id)
        {
            using var client = CreateClient();
            var resp = await client.GetAsync($"Vozilo/Delete/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return ShowError(body, resp.StatusCode);
            }

            var json = await resp.Content.ReadAsStringAsync();
            var vozilo = JsonSerializer.Deserialize<CARSHARE_WEBAPP.Models.Vozilo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(vozilo);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var client = CreateClient();
            var resp = await client.DeleteAsync($"Vozilo/Delete/{id}");
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return ShowError(body, resp.StatusCode);
            }

            return RedirectToAction(nameof(Index));
        }

        private static async Task<string?> FileToBase64Async(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        private HttpClient CreateClient()
        {
            var token = _httpContextAccessor.HttpContext!.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("Korisnik nije prijavljen – JWT nedostaje.");

            var c = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return c;
        }

        public async Task<IActionResult> Details(int id)
        {
            using var client = CreateClient();
            var resp = await client.GetAsync($"Vozilo/GetById/{id}");
            if (!resp.IsSuccessStatusCode)
                return ShowError(await resp.Content.ReadAsStringAsync(), resp.StatusCode);

            var json = await resp.Content.ReadAsStringAsync();

            var vm = JsonSerializer.Deserialize<VoziloVM>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (vm == null)
                return NotFound();

            return View(vm);
        }




        private async Task<HttpResponseMessage> CallApiAsync(
    Func<HttpClient, Task<HttpResponseMessage>> invoker,
    string description)
        {
            using var client = CreateClient();
            var resp = await invoker(client);

            var body = await resp.Content.ReadAsStringAsync();
            _logger.LogInformation(
                "API {Desc}: {Method} {Url} -> {Status}\n{Body}",
                description,
                resp.RequestMessage!.Method,
                resp.RequestMessage.RequestUri,
                (int)resp.StatusCode,
                body);

            return resp;
        }

        private ViewResult ShowError(string? apiBody = null, HttpStatusCode? status = null)
        {
            if (status != null)
                _logger.LogError("API error {Status}: {Body}", (int)status, apiBody);

            return View("Error", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString()
            });
        }

    }
}
