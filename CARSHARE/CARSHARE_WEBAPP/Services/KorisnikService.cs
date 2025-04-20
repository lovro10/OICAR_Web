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

      
        public async Task<HttpResponseMessage> UpdateKorisnikAsync(Korisnik korisnik)
        {

            return await _httpClient.PutAsJsonAsync(ApiUri + $"/{korisnik.IDKorisnik}", korisnik);
        }
        public async Task<KorisnikVM?> GetKorisnikByIdAsync(int id)
        {
            try
            {
                var korisnik = await _httpClient.GetFromJsonAsync<Korisnik>($"{ApiUri}/{id}");

                if (korisnik == null) return null;

                return new KorisnikVM
                {
                    IDKorisnik = korisnik.IDKorisnik,
                    Ime = korisnik.Ime,
                    Prezime = korisnik.Prezime,
                    Email = korisnik.Email,
                    PwdHash = korisnik.PwdHash,
                    PwdSalt = korisnik.PwdSalt,
                    Username = korisnik.Username,
                    Telefon = korisnik.Telefon,
                    DatumRodjenja = korisnik.DatumRodjenja,
                    ImageVozackaID = korisnik.ImageVozackaID,
                    ImageOsobnaID = korisnik.ImageOsobnaID,
                    ImageLiceID = korisnik.ImageLiceID,
                    DeletedAt = korisnik.DeletedAt
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching korisnik by ID: {ex.Message}");
                return null;
            }
        }
       
    }
  }

