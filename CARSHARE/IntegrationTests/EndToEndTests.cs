using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace CARSHARE_WEBAPP.Tests.Integration
{
    public class EndToEndTests :
        IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public EndToEndTests(
            WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services
                        .SingleOrDefault(d => d.ServiceType == typeof(IHttpClientFactory));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddSingleton<IHttpClientFactory>(sp =>
                    {
                        var mockHttp = new MockHttpMessageHandler();

                        mockHttp.When("http://localhost:5194/api/OglasVoznja/GetAll")
                            .Respond("application/json",
                                "[{\"IdOglasVoznja\":1,\"Polaziste\":\"CityA\",\"Odrediste\":\"CityB\"}]");

                        mockHttp.When("http://localhost:5194/api/Poruka/GetMessagesForRide?korisnikVoznjaId=1")
                            .Respond("application/json",
                                "[{\"IdPoruka\":10,\"Content\":\"Hello from driver\",\"Timestamp\":\"2025-06-12T10:00:00\"}]");

                        mockHttp.When("http://localhost:5194/api/Poruka/GetMessagesForRide?korisnikVoznjaId=2")
                            .Respond("application/json",
                                "[]");

                        mockHttp.When("http://localhost:5194/api/Poruka/GetMessagesForRide?korisnikVoznjaId=3")
                            .Respond("application/json",
                                "[{\"IdPoruka\":11,\"Content\":\"Driver here\",\"Timestamp\":\"2025-06-12T11:00:00\"}]");

                        return new FakeHttpClientFactory(new HttpClient(mockHttp)
                        {
                            BaseAddress = new Uri("http://localhost:5194/api/")
                        });
                    });
                });
            });
        }

        [Fact]
        public async Task Index_ReturnsViewWith_OglasVoznja_List()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/OglasVoznja/Index");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("CityA", html);
            Assert.Contains("CityB", html);
        }

        [Fact]
        public async Task IndexUser_WithoutSession_RedirectsToLogin()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            var response = await client.GetAsync("/OglasVoznja/IndexUser");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var redirectUri = new Uri(client.BaseAddress!, response.Headers.Location!);
            Assert.Equal("/Auth/Login", redirectUri.AbsolutePath);
        }

        [Fact]
        public async Task PorukaVoznja_Index_ShowsMessagesAndVozacId()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/PorukaVoznja/Index?korisnikVoznjaId=3&korisnikId=7");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Driver here", html);
            Assert.Contains("name=\"VozacId\" value=\"7\"", html);
        }

        [Fact]
        public async Task PorukaVoznja_Index_ShowsMessagesAndNoPutnikId()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/PorukaVoznja/Index?korisnikVoznjaId=1");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Hello from driver", html);
            Assert.Contains("name=\"VozacId\" value=\"\"", html);
            Assert.Contains("name=\"PutnikId\" value=\"\"", html);
        }

        [Fact]
        public async Task PorukaVoznja_Join_SetsPutnikIdAndNoVozacId()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/PorukaVoznja/Join?korisnikVoznjaId=2&korisnikId=5");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("name=\"PutnikId\" value=\"5\"", html);
            Assert.Contains("name=\"VozacId\" value=\"\"", html);
        }

        public class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public FakeHttpClientFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }
    }
}
