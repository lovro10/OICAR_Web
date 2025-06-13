using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.Services.Interfaces
{
    public interface IKorisnikService
    {
        Task<List<KorisnikVM>> GetKorisniciAsync();
        Task<HttpResponseMessage> UpdateKorisnikAsync(EditKorisnikVM model);
        Task<List<ImageVM>> GetImagesAsync(string jwt);
        Task<HttpResponseMessage> LoginAsync(LoginVM model);

    }
}
