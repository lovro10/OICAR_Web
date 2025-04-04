using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.Services
{
    public class VoziloService
    {
        private readonly HttpClient _httpClient;
        private string ApiUri = "http://localhost:5194/api/Vozilo";
        public VoziloService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<VoziloVM>> GetVozilaAsync()
        {
            List<Vozilo> listaVozila = new List<Vozilo>();

            Vozilo vozilo = new Vozilo 
            { 
                Marka = "BMW", 
                Model = "320D", 
                Registracija = "ZG2736DH" 
            }; 

            string vozilojson = System.Text.Json.JsonSerializer.Serialize(vozilo); 
            var login = await _httpClient.PostAsJsonAsync(ApiUri, vozilojson);  

            var vozila = await _httpClient.GetFromJsonAsync<List<VoziloVM>>(ApiUri); 

            return vozila?.Select(v => new VoziloVM 
            { 
                IDVozilo = v.IDVozilo, 
                Marka = v.Marka, 
                Model = v.Model, 
                Registracija = v.Registracija 
            }).ToList() ?? new List<VoziloVM>();
        }
    }
}
