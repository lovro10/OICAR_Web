using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IntegrationTests
{

    public class EndToEndTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public EndToEndTests(CustomWebApplicationFactory factory)
        {


            var options = new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            };
            _client = factory.CreateClient(options);
        }


        [Fact]
        public async Task GET_Korisnik_GetKorisnici_ReturnsOkAndContainsPageHeader()
        {
            var response = await _client.GetAsync("/Korisnik/GetKorisnici");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Lista Korisnika");
            html.Should().Contain("id=\"usersTable\"");
        }

 
        [Fact]
        public async Task GET_Korisnik_Login_ReturnsOkAndContainsForm()
        {
            var response = await _client.GetAsync("/Korisnik/Login");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("name=\"UserName\"");
            html.Should().Contain("name=\"Password\"");
        }



        [Fact]
        public async Task POST_PorukaVozilo_SendMessage_EmptyMessage_RedirectsBackToIndex()
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Korisnikvoziloid", "1"),
                new KeyValuePair<string, string>("Message", "")          
            });

            var response = await _client.PostAsync("/PorukaVozilo/SendMessage", form);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);  
            response.Headers.Location!.ToString()
                    .Should().Be("/PorukaVozilo?korisnikVoziloId=1");
        }

        [Fact]
        public async Task GET_Vozilo_Index_WithoutAuth_RedirectsToLogin()
            {
            var response = await _client.GetAsync("/Vozilo/Index");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);    
            response.Headers.Location!.ToString()
                     .Should().Be("/Korisnik/Login");
        }

        [Fact]
        public async Task POST_Vozilo_Create_WithoutAuth_RedirectsToLogin()
        {
            var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("Naziv","TestAuto")
                });

            var response = await _client.PostAsync("/Vozilo/Create", form);

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);         
            response.Headers.Location!.ToString()
                    .Should().Be("/Account/Login");
            }
        }

    }

