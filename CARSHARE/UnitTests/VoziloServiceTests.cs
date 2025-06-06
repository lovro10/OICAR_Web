using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using Xunit;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using CARSHARE_WEBAPP.UnitTests.Helpers;

namespace CARSHARE_WEBAPP.UnitTests
{
    public class VoziloServiceTests
    {
        [Fact]
        public async Task GetAllVehiclesAsync_WhenApiReturnsJsonArray_ShouldReturnMappedList()
        {
            var dummyList = new List<object> { new { }, new { } };

            var json = JsonConvert.SerializeObject(dummyList); 

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Vozilo/")
            };

            var service = new VoziloService(httpClient);

            var result = await service.GetAllVehiclesAsync();

            
            result.Should().NotBeNull();
            result.Should().HaveCount(2, because: "the fake API returned an array of two items");
           
        }

        [Fact]
        public async Task GetAllVehiclesAsync_WhenApiReturnsNonSuccess_ShouldReturnEmptyList()
        {
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Vozilo/")
            };

            var service = new VoziloService(httpClient);

            var result = await service.GetAllVehiclesAsync();

            result.Should().NotBeNull();
            result.Should().BeEmpty(
                because: "when response.IsSuccessStatusCode is false, the service returns a new empty List<VoziloVM>()"
            );
        }

        [Fact]
        public async Task GetVehicleByIdAsync_WhenApiReturnsJsonObject_ShouldReturnNonNullVoziloVM()
        {
            var fakeJsonObject = "{}";

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fakeJsonObject, Encoding.UTF8, "application/json")
            };
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Vozilo/")
            };

            var service = new VoziloService(httpClient);

            var result = await service.GetVehicleByIdAsync(123);

            result.Should().NotBeNull(
                because: "the API returned 200 OK with a JSON object (even if empty), which JsonConvert can map to a VoziloVM instance"
            );

        }

        [Fact]
        public async Task GetVehicleByIdAsync_WhenApiReturnsNonSuccess_ShouldReturnNull()
        {
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
            var fakeHandler = new FakeHttpMessageHandler(fakeResponse);
            var httpClient = new HttpClient(fakeHandler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Vozilo/")
            };

            var service = new VoziloService(httpClient);

            var result = await service.GetVehicleByIdAsync(999);

            result.Should().BeNull(
                because: "when response.IsSuccessStatusCode is false, the service returns null"
            );
        }
    }
}
