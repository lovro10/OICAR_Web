using CARSHARE_WEBAPP.ViewModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CARSHARE_WEBAPP.Services
{
    public class ImageService
    {
        private readonly HttpClient _httpClient;

        public ImageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5194/api/Image/");
        }

        public async Task<List<ImageVM>> GetAllImagesAsync(string jwtToken = null)
        {
            if (!string.IsNullOrEmpty(jwtToken))
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtToken);

            var resp = await _httpClient.GetAsync("GetImages");
            if (!resp.IsSuccessStatusCode)
                return new List<ImageVM>();

            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ImageVM>>(json)
                   ?? new List<ImageVM>();
        }
        public async Task<List<ImageVM>> GetImagesForVehicleAsync(int vehicleId)
        {
            var resp = await _httpClient.GetAsync($"GetImagesForVehicle?vehicleId={vehicleId}");
            if (!resp.IsSuccessStatusCode) return new();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<ImageVM>>(json) ?? new();
        }

    }
}
