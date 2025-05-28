using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using AspNetCoreGeneratedDocument;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CARSHARE_WEBAPP.Services
{
    public class KorisnikService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private string ApiUri = "http://localhost:5194/api/Korisnik";
        public KorisnikService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<List<KorisnikVM>> GetKorisniciAsync()
        {


            List<KorisnikVM> korisnici = new List<KorisnikVM>();
            try
            {
                var response = await _httpClient.GetStringAsync(ApiUri);
                korisnici = JsonConvert.DeserializeObject<List<KorisnikVM>>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching korisnici: {ex.Message}");
            }
            

            return korisnici?               
                .Select(k => new KorisnikVM
            {
                IDKorisnik = k.IDKorisnik,
                Ime = k.Ime,
                Prezime = k.Prezime,
                Email = k.Email,
                PwdHash = k.PwdHash,
                PwdSalt = k.PwdSalt,
                Username = k.Username,
                Telefon = k.Telefon,
                DatumRodjenja = k.DatumRodjenja,
                IsConfirmed = k.IsConfirmed,
                Ulogaid = k.Ulogaid,
                Uloga = k.Uloga

            }).ToList() ?? new List<KorisnikVM>();
        }

      
        public async Task<HttpResponseMessage> UpdateKorisnikAsync(EditKorisnikVM korisnik)
        {
           
            return await _httpClient.PutAsJsonAsync<EditKorisnikVM>($"{ApiUri}/{korisnik.IDKorisnik}", korisnik);
        }
        public async Task<Korisnik?> GetKorisnikByIdAsync(int id)
        {
            try
            {
                var korisnik = await _httpClient.GetFromJsonAsync<Korisnik>($"{ApiUri}/{id}");

                if (korisnik == null) return null;

                return new Korisnik
                {
                    Idkorisnik = korisnik.Idkorisnik,
                    Ime = korisnik.Ime,
                    Prezime = korisnik.Prezime,
                    Email = korisnik.Email,
                    Pwdhash = korisnik.Pwdhash,
                    Pwdsalt = korisnik.Pwdsalt,
                    Username = korisnik.Username,
                    Telefon = korisnik.Telefon,
                    Datumrodjenja = korisnik.Datumrodjenja,
                    Imagevozackaid = korisnik.Imagevozackaid,
                    Imageosobnaid = korisnik.Imageosobnaid,
                    Imageliceid = korisnik.Imageliceid,
                    Deletedat = korisnik.Deletedat 
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching korisnik by ID: {ex.Message}");
                return null;
            }
        }
        public async Task<List<ImageVM>> GetImagesAsync(string jwtToken = null)
        {
            if (!string.IsNullOrEmpty(jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization
                  = new AuthenticationHeaderValue("Bearer", jwtToken);
            }
            _httpClient.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync("http://localhost:5194/api/Image");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var images = JsonSerializer.Deserialize<List<ImageVM>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return images ?? new List<ImageVM>();
        }

    }
  }

