using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.Services.Interfaces;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests
{

    public class CustomWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
              .UseEnvironment("Testing")
              .ConfigureTestServices(services =>
              {
                  var realKorisnik = services.Single(
                      d => d.ServiceType == typeof(IKorisnikService));
                  services.Remove(realKorisnik);
                  services.AddScoped<IKorisnikService, FakeKorisnikService>();

              });
        }
    }

    public class FakeKorisnikService : IKorisnikService
    {
        public Task<List<KorisnikVM>> GetKorisniciAsync()
        {
            var list = new List<KorisnikVM>
        {
            new KorisnikVM { IDKorisnik = 1, Ime = "Test User" }
        };
            return Task.FromResult(list);
        }

        public Task<HttpResponseMessage> UpdateKorisnikAsync(EditKorisnikVM model)
        {
            
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"success\":true,\"id\":{model.IDKorisnik}}}")
            };
            return Task.FromResult(response);
        }

        public Task<List<ImageVM>> GetImagesAsync(string jwt)
        {

            return Task.FromResult(new List<ImageVM>());
        }
    }
}
