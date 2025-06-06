// File: CARSHARE_WEBAPP/Services/Interfaces/IKorisnikService.cs

using System.Collections.Generic;
using System.Net.Http;               // <-- for HttpResponseMessage
using System.Threading.Tasks;
using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.Services.Interfaces
{
    public interface IKorisnikService
    {
        Task<List<KorisnikVM>> GetKorisniciAsync();
        Task<HttpResponseMessage> UpdateKorisnikAsync(EditKorisnikVM korisnik);
        Task<List<ImageVM>> GetImagesAsync(string jwtToken = null);
    }
}
