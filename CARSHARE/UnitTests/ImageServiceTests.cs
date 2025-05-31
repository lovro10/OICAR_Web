
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


    public class ImageServiceTests
    {
        [Fact]
        public async Task GetAllImagesAsync_WithValidJwt_ShouldSetAuthorizationHeaderAndReturnList()
        {
            // Arrange
            var dummyList = new List<ImageVM>
            {
                new ImageVM
                {
                    IDImage = 1,
                    Name = "image1.png",
                    Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("content1")),
                    Content = Encoding.UTF8.GetBytes("content1")
                },
                new ImageVM
                {
                    IDImage = 2,
                    Name = "image2.png",
                    Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("content2")),
                    Content = Encoding.UTF8.GetBytes("content2")
                }
            };
            string json = JsonConvert.SerializeObject(dummyList);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Image/")
            };

            var service = new ImageService(httpClient);
            string testJwt = "valid.jwt.token";

            var result = await service.GetAllImagesAsync(testJwt);

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri("http://localhost:5194/api/Image/GetImages"));

            handler.LastRequest.Headers.Authorization.Should().NotBeNull();
            handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest.Headers.Authorization.Parameter.Should().Be(testJwt);

            result.Should().NotBeNull();
            result.Should().HaveCount(2, "because the fake API returned two items");
            result[0].IDImage.Should().Be(1);
            result[1].IDImage.Should().Be(2);
        }

        [Fact]
        public async Task GetAllImagesAsync_NoJwt_ShouldNotSetAuthorizationHeaderButReturnList()
        {
            // Arrange
            var dummyList = new List<ImageVM>
            {
                new ImageVM
                {
                    IDImage = 5,
                    Name = "picture.png",
                    Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("pic")),
                    Content = Encoding.UTF8.GetBytes("pic")
                }
            };
            string json = JsonConvert.SerializeObject(dummyList);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Image/")
            };

            var service = new ImageService(httpClient);

            var result = await service.GetAllImagesAsync(null);

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri("http://localhost:5194/api/Image/GetImages"));

            handler.LastRequest.Headers.Authorization.Should().BeNull("because we passed null JWT");

            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].IDImage.Should().Be(5);
        }

        // 3) GetAllImagesAsync - non-success status
        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task GetAllImagesAsync_WhenApiReturnsNonSuccess_ShouldReturnEmptyList(HttpStatusCode statusCode)
        {
            var fakeResponse = new HttpResponseMessage(statusCode);
            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Image/")
            };

            var service = new ImageService(httpClient);

            var result = await service.GetAllImagesAsync("some.jwt");

            
            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri("http://localhost:5194/api/Image/GetImages"));

            handler.LastRequest.Headers.Authorization.Should().NotBeNull();
            handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest.Headers.Authorization.Parameter.Should().Be("some.jwt");

            result.Should().NotBeNull();
            result.Should().BeEmpty("because the API returned a non-success status code");
        }

        // 4) GetImagesForVehicleAsync - success path
        [Fact]
        public async Task GetImagesForVehicleAsync_WhenApiReturnsJsonArray_ShouldReturnList()
        {
            var vehicleId = 42;
            var dummyList = new List<ImageVM>
            {
                new ImageVM
                {
                    IDImage = 10,
                    Name = "v42_img1.jpg",
                    Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("abc")),
                    Content = Encoding.UTF8.GetBytes("abc")
                },
                new ImageVM
                {
                    IDImage = 11,
                    Name = "v42_img2.jpg",
                    Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("def")),
                    Content = Encoding.UTF8.GetBytes("def")
                }
            };
            string json = JsonConvert.SerializeObject(dummyList);

            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Image/")
            };

            var service = new ImageService(httpClient);

            var result = await service.GetImagesForVehicleAsync(vehicleId);

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri($"http://localhost:5194/api/Image/GetImagesForVehicle?vehicleId={vehicleId}"));

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].IDImage.Should().Be(10);
            result[1].IDImage.Should().Be(11);
        }

        // 5) GetImagesForVehicleAsync - non-success status
        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.BadRequest)]
        public async Task GetImagesForVehicleAsync_WhenApiReturnsNonSuccess_ShouldReturnEmptyList(HttpStatusCode statusCode)
        {
            var vehicleId = 99;
            var fakeResponse = new HttpResponseMessage(statusCode);
            var handler = new CapturingHandler(fakeResponse);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/Image/")
            };

            var service = new ImageService(httpClient);

            var result = await service.GetImagesForVehicleAsync(vehicleId);

            handler.LastRequest.Should().NotBeNull();
            handler.LastRequest!.RequestUri.Should().Be(new Uri($"http://localhost:5194/api/Image/GetImagesForVehicle?vehicleId={vehicleId}"));

            result.Should().NotBeNull();
            result.Should().BeEmpty("because the API returned a non-success status code");
        }
    }
}
