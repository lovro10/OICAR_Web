﻿using CARSHARE_WEBAPP.ViewModels;
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
        public async Task<IActionResult> Index(int oglasVoznjaId, int? korisnikId)
        {
            var response = await _httpClient.GetAsync($"Poruka/GetMessagesForRide?oglasVoznjaId={oglasVoznjaId}"); 

            var messages = new List<PorukaVoznjaGetVM>(); 
            if (response.IsSuccessStatusCode) 
            {
                messages = await response.Content.ReadFromJsonAsync<List<PorukaVoznjaGetVM>>();
            } 

            var vm = new PorukaVoznjaSendVM
            { 
                Oglasvoznjaid = oglasVoznjaId,
                PutnikId = null,
                VozacId = korisnikId,
                Messages = messages!
            }; 

            return View(vm);
        } 

        [HttpGet]
        public async Task<IActionResult> Join(int oglasVoznjaId, int? korisnikId)
        { 
            var response = await _httpClient.GetAsync($"Poruka/GetMessagesForRide?oglasVoznjaId={oglasVoznjaId}");

            var messages = new List<PorukaVoznjaGetVM>(); 
            if (response.IsSuccessStatusCode) 
            { 
                messages = await response.Content.ReadFromJsonAsync<List<PorukaVoznjaGetVM>>();
            } 

            var vm = new PorukaVoznjaSendVM
            {
                Oglasvoznjaid = oglasVoznjaId,
                PutnikId = korisnikId,
                VozacId = null,
                Messages = messages!
            }; 

            return View(vm);
        } 

        [HttpPost]
        public async Task<IActionResult> SendMessage(PorukaVoznjaSendVM porukaVM)
        {
            if (string.IsNullOrWhiteSpace(porukaVM.Message))
            {
                ModelState.AddModelError("", "Message cannot be empty.");
                return RedirectToAction("Index", new
                {
                    OglasVoznjaId = porukaVM.Oglasvoznjaid,
                    PutnikId = porukaVM.PutnikId,
                    VozacId = porukaVM.VozacId
                });
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            string? role = HttpContext.Session.GetString("Role");
            if (role == null)
                return RedirectToAction("Login", "Auth");

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
                OglasVoznjaId = porukaVM.Oglasvoznjaid,
                Content = porukaVM.Message,
                PutnikId = putnikId,
                VozacId = vozacId
            };

            var response = await _httpClient.PostAsJsonAsync("Poruka/SendMessageForRide", sendPayload);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to send message.");
            }

            return RedirectToAction("Index", new
            {
                OglasVoznjaId = porukaVM.Oglasvoznjaid,
                PutnikId = putnikId,
                VozacId = vozacId
            });
        }
    }
}
