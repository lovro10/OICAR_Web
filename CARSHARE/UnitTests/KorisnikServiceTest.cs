using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Newtonsoft.Json;
using Xunit;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;     
using CARSHARE_WEBAPP.Models;         
using CARSHARE_WEBAPP.UnitTests.Helpers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CARSHARE_WEBAPP.UnitTests
{
    public class KorisnikServiceTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public KorisnikServiceTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        }

        [Fact]
        public async Task GetKorisniciAsync_WhenApiReturnsValidJson_ShouldReturnMappedList()
        {
            var dummyList = new List<KorisnikVM>
            {
                new KorisnikVM
                {
                    IDKorisnik = 1,
                    Ime = "Ivan",
                    Prezime = "Horvat",
                    Email = "ivan@example.com",
                    PwdHash = "hash1",
                    PwdSalt = "salt1",
                    Username = "ivanh",
                    Telefon = "0911111111",
                    DatumRodjenja = new DateOnly(2000, 1, 1),    
                    IsConfirmed = true,
                    Ulogaid = 2,
                    Uloga = new Uloga { Iduloga = 2, Naziv = "User" }
                },
                new KorisnikVM
                {
                    IDKorisnik = 2,
                    Ime = "Ana",
                    Prezime = "Kovač",
                    Email = "ana@example.com",
                    PwdHash = "hash2",
                    PwdSalt = "salt2",
                    Username = "anak",
                    Telefon = "0922222222",
                    DatumRodjenja = new DateOnly(1985,5,5),
                    IsConfirmed = false,
                    Ulogaid = 1,
                    Uloga = new Uloga { Iduloga = 1, Naziv = "Admin" }
                }
            };

            var json = JsonConvert.SerializeObject(dummyList);
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };

            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            var result = await service.GetKorisniciAsync();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].IDKorisnik.Should().Be(1);
            result[0].Ime.Should().Be("Ivan");
            result[0].Prezime.Should().Be("Horvat");
            result[0].Username.Should().Be("ivanh");
            result[0].Uloga.Naziv.Should().Be("User");

            result[1].IDKorisnik.Should().Be(2);
            result[1].Username.Should().Be("anak");
            result[1].Uloga.Naziv.Should().Be("Admin");
        }

        [Fact]
        public async Task GetKorisniciAsync_WhenApiThrowsException_ShouldReturnEmptyList()
        {
            var fakeHandler = new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
            );
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };

            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            var result = await service.GetKorisniciAsync();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateKorisnikAsync_ShouldReturnHttpResponseFromPut()
        {
            var sampleEdit = new EditKorisnikVM
            {
                IDKorisnik = 5,
                Ime = "Test",
                Prezime = "User",
                Email = "test@example.com",
                Telefon = "0910000000",
                DatumRodjenja = new DateOnly(2000, 1, 1),
                Username = "testuser"
            };

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.NoContent);
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };

            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            var response = await service.UpdateKorisnikAsync(sampleEdit);

            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task GetKorisnikByIdAsync_WhenApiReturnsNull_ShouldReturnNull()
        {

            var emptyContentResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };
            var fakeHandler = new FakeHttpMessageHandler(emptyContentResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };
            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            var result = await service.GetKorisnikByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetKorisnikByIdAsync_WhenApiReturnsObject_ShouldReturnMappedKorisnik()
        {

            var apiPayload = new Dictionary<string, object>
                {
                    { "Idkorisnik", 42 },
                    { "Ime", "Petar" },
                    { "Prezime", "Perić" },
                    { "Email", "petar@example.com" },
                    { "Pwdhash", "hash" },
                    { "Pwdsalt", "salt" },
                    { "Username", "petarp" },
                    { "Telefon", "0914444444" },
                    { "Datumrodjenja", "1995-07-07" }
                };


            var serialized = System.Text.Json.JsonSerializer.Serialize(apiPayload);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(serialized, Encoding.UTF8, "application/json")
            };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };
            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            var result = await service.GetKorisnikByIdAsync(42);

            result.Should().NotBeNull();
            result!.Idkorisnik.Should().Be(42);
            result.Ime.Should().Be("Petar");
            result.Username.Should().Be("petarp");
            result.Datumrodjenja.Should().Be(new DateOnly(1995, 7, 7));
        }

        [Fact]
        public async Task GetImagesAsync_WhenApiReturnsImagesJson_ShouldReturnImageVMList()
        {
            var dummyImages = new List<ImageVM>
    {
        new ImageVM
        {
            IDImage = 1,
            Name = "DriverLicense.png",
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("dummy‐data‐1")),
            Content = Encoding.UTF8.GetBytes("dummy‐bytes‐1")
        },
        new ImageVM
        {
            IDImage = 2,
            Name = "PersonalID.jpg",
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("dummy‐data‐2")),
            Content = Encoding.UTF8.GetBytes("dummy‐bytes‐2")
        }
    };

            var serialized = System.Text.Json.JsonSerializer.Serialize(dummyImages);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(serialized, Encoding.UTF8, "application/json")
            };

            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };

            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            var result = await service.GetImagesAsync("dummy.jwt.token");

            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result[0].IDImage.Should().Be(1);
            result[0].Name.Should().Be("DriverLicense.png");
            result[0].Base64Content.Should().Be(dummyImages[0].Base64Content);

            result[1].IDImage.Should().Be(2);
            result[1].Name.Should().Be("PersonalID.jpg");
            result[1].Base64Content.Should().Be(dummyImages[1].Base64Content);
        }

        [Fact]
        public async Task GetImagesAsync_WhenApiReturnsNonSuccess_ShouldThrow()
        {
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/")
            };
            var service = new KorisnikService(httpClient, _mockHttpContextAccessor.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetImagesAsync("bad‐token"));
        }
    }
}
