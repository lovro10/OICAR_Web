using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CARSHARE_WEBAPP.Controllers
{
    public class VoziloController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VoziloController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
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
                return View("Error");

            var json = await response.Content.ReadAsStringAsync();
            var vozila = JsonSerializer.Deserialize<List<VoziloVM>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return View(vozila);
        }



    }
}
