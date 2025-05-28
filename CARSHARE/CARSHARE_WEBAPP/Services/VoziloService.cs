using CARSHARE_WEBAPP.ViewModels;
using Newtonsoft.Json;
using System.Net.Http;

namespace CARSHARE_WEBAPP.Services
{
    public class VoziloService
    {
        private readonly HttpClient _httpClient;

        public VoziloService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5194/api/Vozilo/");
        }

        public async Task<List<VoziloVM>> GetAllVehiclesAsync()
        {
            var resp = await _httpClient.GetAsync("GetVehicles");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<VoziloVM>>(json);
                return list ?? new List<VoziloVM>();
            }
            return new List<VoziloVM>();
        }

        public async Task<VoziloVM?> GetVehicleByIdAsync(int id)
        {
            var resp = await _httpClient.GetAsync($"Details?id={id}");
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadAsStringAsync();
                var vm = JsonConvert.DeserializeObject<VoziloVM>(json);
                return vm;
            }
            return null;
        }
    }
}
