using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IntegrationTests
{
    /*
     * starta se prava aplikaciju u memoriji pomocu WebApplicationFactory,
     * koristimo inmemory database za testiranje i salje http zahtjeve kroz cijelu applikaciju
     */
    public class EndToEndTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public EndToEndTests(CustomWebApplicationFactory factory)
        {
            /*
             * if the app returns a 302 Redirect,
             * the StatusCode remains 302 and response.
             * Headers.Location tells us exactly where it wanted to send the browser next
             */

            var options = new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            };
            _client = factory.CreateClient(options);
        }

        /*
         * We are testing that home page is publicly accessible and returns HTTP 200 ok
         * and that the rendered HTML page includes <title>
         * 
         * Sto zapravo testiramo -> Da li se aplikacija start up-a i da li se rendera html stranica
         */

        [Fact]
        public async Task GET_Home_Index_ReturnsStatus200AndContainsTitle()
        {
            var response = await _client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("<title>");
        }

        /*
         * Sto zapravo testiramo -> da li je /Korisnik/GetKorisnici javno dostupno i da li prikazuje
         * ocekivanu tablicu sa listom korisnika
         */
        [Fact]
        public async Task GET_Korisnik_GetKorisnici_ReturnsOkAndContainsPageHeader()
        {
            var response = await _client.GetAsync("/Korisnik/GetKorisnici");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Lista Korisnika");
            html.Should().Contain("id=\"usersTable\"");
        }

        /*
         * Sto zapravo testiramo -> da li je login stranica javno dostupna i da li vraca login formu
         * sa dva input fielda
         */
        [Fact]
        public async Task GET_Korisnik_Login_ReturnsOkAndContainsForm()
        {
            var response = await _client.GetAsync("/Korisnik/Login");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("name=\"UserName\"");
            html.Should().Contain("name=\"Password\"");
        }

        /*
         * Sto zapravo testiramo -> /Vozilo je protected, ako nema validnog JWTokena,
         * kontroler bi trebao redirectati usera na home page
         */
        [Fact]
        public async Task GET_Vozilo_Index_WhenUnauthenticated_RedirectsToRoot()
        {
            var response = await _client.GetAsync("/Vozilo");

            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location.OriginalString.Should().Be("/");
        }

        /*
         * Sto zapravo testiramo -> ako pritisnemo details za usera koji nema ID,
         * kontroler bi trebao vratiti HTTP 404 not found
         */
        [Fact]
        public async Task GET_Korisnik_Details_Nonexistent_ReturnsNotFound()
        {
            var response = await _client.GetAsync("/Korisnik/Details/9999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


    }
}
