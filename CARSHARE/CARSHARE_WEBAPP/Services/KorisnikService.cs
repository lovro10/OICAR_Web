using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.Net.Http.Json;
using System.Text.Json;
using Newtonsoft.Json;
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
            List<Korisnik> listakorisnika= new List<Korisnik>();
        
            Korisnik korisnik = new Korisnik{
                Username = "klaljek",
                PwdHash = ""
            };
            
            string korisnikjson = System.Text.Json.JsonSerializer.Serialize(korisnik); 
            var login = await  _httpClient.PostAsJsonAsync(ApiUri,korisnikjson); 

            var korisnici = await _httpClient.GetFromJsonAsync<List<KorisnikVM>>(ApiUri);

        

           return korisnici?.Select(k => new KorisnikVM
            {
                IDKorisnik = k.IDKorisnik,
                Ime = k.Ime,
                Prezime = k.Prezime,
                Email = k.Email,
                Username = k.Username,
                Telefon = k.Telefon,
                DatumRodjenja = k.DatumRodjenja,
                IsConfirmed = k.IsConfirmed,                
                DeletedAt = k.DeletedAt
            }).ToList() ?? new List<KorisnikVM>();
        }

    }
}
