using CARSHARE_WEBAPP.ViewModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CARSHARE_WEBAPP.Services
{
    public class VoznjaService
    {
        private readonly HttpClient _httpClient;

        public VoznjaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            //  → bazna ruta na tvoj API controller
            _httpClient.BaseAddress = new Uri("http://localhost:5194/api/OglasVoznja/");
        }

        public async Task<List<VoznjaVM>> GetVoznjeAsync(string jwtToken)
        {
            if (!string.IsNullOrEmpty(jwtToken))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);

            var response = await _httpClient.GetAsync("GetAll");
            if (!response.IsSuccessStatusCode)
                return new List<VoznjaVM>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<VoznjaVM>>(json)
                   ?? new List<VoznjaVM>();
        }
    }
}
