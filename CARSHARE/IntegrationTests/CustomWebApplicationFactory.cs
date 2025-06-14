using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.Services.Interfaces;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
                  var descriptor = services
                      .Single(d => d.ServiceType == typeof(DbContextOptions<CarshareContext>));
                  services.Remove(descriptor);

                  services.AddDbContext<CarshareContext>(opts =>
                  {
                      opts.UseInMemoryDatabase("TestingDb");
                  });

                  var realKorisnik = services.Single(
                      d => d.ServiceType == typeof(IKorisnikService));
                  services.Remove(realKorisnik);
                  services.AddScoped<IKorisnikService, FakeKorisnikService>();

                  services.AddHttpClient("KorisnikClient")
                    .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());
                  

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
        public Task<HttpResponseMessage> LoginAsync(LoginVM model)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            return Task.FromResult(response);
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
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.ToString().Contains("Details?id=1"))
            {
                var korisnik = new KorisnikVM
                {
                    IDKorisnik = 1,
                    Ime = "Test",
                    Prezime = "User",
                    Username = "Username"
                };

                var json = JsonConvert.SerializeObject(korisnik);

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
            });
        }
    }
}