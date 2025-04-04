using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.Services
{
    public class VoznjaService
    {
        private readonly HttpClient _httpClient;
        private string ApiUri = "http://localhost:5194/api/Voznja";
        public VoznjaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<VoznjaVM>> GetVoznjaAsync() 
        { 
            List<Oglasvoznja> listVoznja = new List<Oglasvoznja>(); 

            Oglasvoznja oglasVoznja = new Oglasvoznja 
            { 
                Voziloid = 1,
                DatumIVrijemePolaska = DateTime.Now,
                DatumIVrijemeDolaska = DateTime.Now, 
                Troskoviid = 1, 
                BrojPutnika = 2, 
                Statusvoznjeid = 1,   
                Lokacijaid = 1 
            }; 

            string voznjajson = System.Text.Json.JsonSerializer.Serialize(oglasVoznja); 
            var voznja = await _httpClient.PostAsJsonAsync(ApiUri, voznjajson); 

            var voznje = await _httpClient.GetFromJsonAsync<List<VoznjaVM>>(ApiUri);

            return voznje?.Select(o => new VoznjaVM
            { 
                Idoglasvoznja = o.Idoglasvoznja, 
                Voziloid = o.Voziloid,
                DatumIVrijemePolaska = o.DatumIVrijemePolaska, 
                DatumIVrijemeDolaska = o.DatumIVrijemeDolaska,  
                Troskoviid = o.Troskoviid,
                BrojPutnika = o.BrojPutnika,
                Statusvoznjeid = o.Statusvoznjeid,
                Lokacijaid = o.Lokacijaid 
            }).ToList() ?? new List<VoznjaVM>();
        }
    }
}
