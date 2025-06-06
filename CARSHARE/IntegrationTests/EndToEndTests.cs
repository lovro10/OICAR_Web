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
        public async Task GET_Home_Index_ReturnsStatus200AndContainsTitle()
        {
            var response = await _client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("<title>");
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
    }
}
