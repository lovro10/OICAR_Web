
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.UnitTests
{
    
    public class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _responseToReturn;

        
        public HttpRequestMessage? LastRequest { get; private set; }

        public CapturingHandler(HttpResponseMessage responseToReturn)
        {
            _responseToReturn = responseToReturn;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responseToReturn);
        }
    }

    public class VoznjaServiceTests
    {
        [Fact]
        public async Task GetVoznjeAsync_WithValidJwt_ShouldSetAuthorizationHeaderAndReturnList()
        {
            var dummyList = new List<VoznjaVM>
            {
                new VoznjaVM { Idoglasvoznja = 1, Voziloid = 10, DatumIVrijemePolaska = DateTime.Now, DatumIVrijemeDolaska = DateTime.Now.AddHours(1), Troskoviid = 5, BrojPutnika = 3, Statusvoznjeid = 2, Lokacijaid = 7 },
                new VoznjaVM { Idoglasvoznja = 2, Voziloid = 11, DatumIVrijemePolaska = DateTime.Now, DatumIVrijemeDolaska = DateTime.Now.AddHours(2), Troskoviid = 6, BrojPutnika = 4, Statusvoznjeid = 3, Lokacijaid = 8 }
            };
            string json = JsonConvert.SerializeObject(dummyList);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/OglasVoznja/")
            };

            var service = new VoznjaService(httpClient);
            string testJwt = "dummy.jwt.token";

            var result = await service.GetVoznjeAsync(testJwt);

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri("http://localhost:5194/api/OglasVoznja/GetAll"));

            handler.LastRequest.Headers.Authorization.Should().NotBeNull();
            handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest.Headers.Authorization.Parameter.Should().Be(testJwt);

            result.Should().NotBeNull();
            result.Should().HaveCount(2, "because the fake API returned an array of two items");
            result[0].Idoglasvoznja.Should().Be(1);
            result[1].Idoglasvoznja.Should().Be(2);
        }


        [Fact]
        public async Task GetVoznjeAsync_NoJwt_ShouldNotSetAuthorizationHeaderButReturnList()
        {
            var dummyList = new List<VoznjaVM>
            {
                new VoznjaVM { Idoglasvoznja = 5, Voziloid = 20, DatumIVrijemePolaska = DateTime.Now, DatumIVrijemeDolaska = DateTime.Now.AddMinutes(30), Troskoviid = 8, BrojPutnika = 2, Statusvoznjeid = 4, Lokacijaid = 10 }
            };
            string json = JsonConvert.SerializeObject(dummyList);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/OglasVoznja/")
            };

            var service = new VoznjaService(httpClient);

            var result = await service.GetVoznjeAsync("");

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri("http://localhost:5194/api/OglasVoznja/GetAll"));

            handler.LastRequest.Headers.Authorization.Should().BeNull("because we passed an empty JWT, so no Authorization header should be added");

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Idoglasvoznja.Should().Be(5);
        }


        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetVoznjeAsync_WhenApiReturnsNonSuccess_ShouldReturnEmptyList(HttpStatusCode statusCode)
        {
            var fakeResponse = new HttpResponseMessage(statusCode);
            var handler = new CapturingHandler(fakeResponse);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/OglasVoznja/")
            };

            var service = new VoznjaService(httpClient);

            var result = await service.GetVoznjeAsync("any.jwt.token");

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri("http://localhost:5194/api/OglasVoznja/GetAll"));

            handler.LastRequest.Headers.Authorization.Should().NotBeNull();
            handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest.Headers.Authorization.Parameter.Should().Be("any.jwt.token");

            result.Should().NotBeNull();
            result.Should().BeEmpty("because the API returned a non‐success status code");
        }
    }
}
