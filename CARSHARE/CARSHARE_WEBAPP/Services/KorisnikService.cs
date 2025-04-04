using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using AspNetCoreGeneratedDocument;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Services
{
    public class KorisnikService
    {
        private readonly HttpClient _httpClient;
        private string ApiUri = "http://localhost:5194/api/Korisnik";
        public KorisnikService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<KorisnikVM>> GetKorisniciAsync()
        {

            List<Korisnik> korisnici = new List<Korisnik>();
            try
            {
                korisnici = await _httpClient.GetFromJsonAsync<List<Korisnik>>(ApiUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching korisnici: {ex.Message}");
            }

            return korisnici?.Select(k => new KorisnikVM
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
                DeletedAt = k.DeletedAt
            }).ToList() ?? new List<KorisnikVM>();
        }

    }
}
