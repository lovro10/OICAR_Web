using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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

            response.Content.Headers.ContentType?.MediaType
                .Should().Be("text/html");

            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("Lista Korisnika");
            html.Should().Contain("id=\"usersTable\"");

            response.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        [Fact]
        public async Task GET_Korisnik_Login_ReturnsOkAndContainsForm()
        {
            var response = await _client.GetAsync("/Korisnik/Login");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            response.Content.Headers.ContentType?.MediaType
                .Should().Be("text/html");

            var html = await response.Content.ReadAsStringAsync();
            html.Should().Contain("name=\"UserName\"");
            html.Should().Contain("name=\"Password\"");

            response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
            cookies.Should().Contain(c => c.StartsWith
                (".AspNetCore.Antiforgery.", StringComparison.OrdinalIgnoreCase),
                "an antiforgery cookie with a dynamic suffix should be issued");

        }

        [Fact]
        public async Task POST_Korisnik_Login_WithInvalidCredentials_Fails()
        {
            var get = await _client.GetAsync("/Korisnik/Login");
            get.StatusCode.Should().Be(HttpStatusCode.OK);

            var antiCookie = get.Headers
                .GetValues("Set-Cookie")
                .First(h => h.StartsWith(".AspNetCore.Antiforgery"));
            _client.DefaultRequestHeaders.Add("Cookie", antiCookie);

            var html = await get.Content.ReadAsStringAsync();
            var token = Regex.Match(html,
                @"<input name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""")
                .Groups[1].Value;

            var form = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string,string>("UserName", "nosuchuser"),
                new KeyValuePair<string,string>("Password", "wrongpass"),
                new KeyValuePair<string,string>("__RequestVerificationToken", token),
                });
            var post = await _client.PostAsync("/Korisnik/Login", form);

            post.StatusCode.Should().Be(HttpStatusCode.OK);
            post.Content.Headers.ContentType?.MediaType
                .Should().Be("text/html");

            var postHtml = await post.Content.ReadAsStringAsync();
            postHtml.Should().Contain("Invalid login attempt");        
            postHtml.Should().Contain("name=\"UserName\"");             
            postHtml.Should().Contain("name=\"Password\"");

            post.Headers.TryGetValues("Set-Cookie", out var cookies)
                .Should().BeTrue();
            cookies.Should().NotContain(c => c.StartsWith(".AspNetCore.Cookies"));
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

            response.Content.Headers.ContentType.Should().BeNull();

            response.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        [Fact]
        public async Task GET_Vozilo_Index_WithoutAuth_RedirectsToLogin()
        {
            var response = await _client.GetAsync("/Vozilo/Index");
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            response.Headers.Location!.ToString()
                .Should().Be("/Korisnik/Login");

            response.Content.Headers.ContentType.Should().BeNull();

            response.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        [Fact]
        public async Task POST_Vozilo_Create_WithoutAuth_RedirectsToLogin()
        {
            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Naziv", "TestAuto")
            });

            var response = await _client.PostAsync("/Vozilo/Create", form);
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);

            response.Headers.Location!.ToString()
                .Should().Be("/Account/Login");

            response.Content.Headers.ContentType.Should().BeNull();

            response.Headers.Contains("Set-Cookie").Should().BeFalse();
        }
    }
}
