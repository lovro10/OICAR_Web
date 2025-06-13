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
using JsonException = System.Text.Json.JsonException;

namespace CARSHARE_WEBAPP.Services
{
    public class KorisnikService : Interfaces.IKorisnikService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private string ApiUri = "http://localhost:5194/api/Korisnik";
        public KorisnikService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<HttpResponseMessage> LoginAsync(LoginVM model)
        {
            return await _httpClient.PostAsJsonAsync("Login", model);
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
            var response = await _httpClient.GetAsync($"api/korisnici/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            var jsonString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonString))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;



                int idKor = root.GetProperty("Idkorisnik").GetInt32();
                string ime = root.GetProperty("Ime").GetString()!;
                string prezime = root.GetProperty("Prezime").GetString()!;
                string email = root.GetProperty("Email").GetString()!;
                string pwdHash = root.GetProperty("Pwdhash").GetString()!;
                string pwdSalt = root.GetProperty("Pwdsalt").GetString()!;
                string username = root.GetProperty("Username").GetString()!;
                string telefon = root.GetProperty("Telefon").GetString()!;
                string datumStr = root.GetProperty("Datumrodjenja").GetString()!; // e.g. "1995-07-07"

                var datum = DateOnly.ParseExact(datumStr, "yyyy-MM-dd");

                var korisnik = new Korisnik
                {
                    Idkorisnik = idKor,
                    Ime = ime,
                    Prezime = prezime,
                    Email = email,
                    Pwdhash = pwdHash,
                    Pwdsalt = pwdSalt,
                    Username = username,
                    Telefon = telefon,
                    Datumrodjenja = datum,
                };

                return korisnik;
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
            catch (JsonException)
            {
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

