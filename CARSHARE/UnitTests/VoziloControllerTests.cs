using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CARSHARE_WEBAPP.UnitTests
{
  
    internal class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _storage.Keys;

        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable { get; } = true;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Clear() => _storage.Clear();

        public void Remove(string key)
        {
            _storage.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _storage[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _storage.TryGetValue(key, out value);
        }
    }

    public class FakeHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpResponseMessage _fakeResponse;

        public FakeHttpMessageHandler(HttpResponseMessage fakeResponse)
        {
            _fakeResponse = fakeResponse ?? throw new ArgumentNullException(nameof(fakeResponse));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_fakeResponse);
        }
    }

    public class VoziloControllerTests
    {
        private const string FakeJwtToken = "fake-jwt-token";

  
        private static Mock<IHttpContextAccessor> BuildHttpContextAccessorMock(string? token = FakeJwtToken)
        {
            var fakeSession = new FakeSession();
            if (!string.IsNullOrEmpty(token))
            {
                var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
                fakeSession.Set("JWToken", tokenBytes);
            }

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(ctx => ctx.Session).Returns(fakeSession);

            var accessorMock = new Mock<IHttpContextAccessor>();
            accessorMock.Setup(acc => acc.HttpContext).Returns(httpContextMock.Object);

            return accessorMock;
        }

     
        private class TestableVoziloController : VoziloController
        {
            private readonly HttpClient _httpClient;

            public TestableVoziloController(
                IHttpContextAccessor httpContextAccessor,
                ILogger<VoziloController> logger,
                HttpClient httpClient
            ) : base(httpContextAccessor, logger)
            {
                _httpClient = httpClient;
            }

            protected override HttpClient CreateClient()
            {
                return _httpClient;
            }
        }

        private static ILogger<VoziloController> BuildLogger() =>
            new Mock<ILogger<VoziloController>>().Object;


        [Fact]
        public async Task Index_NoJwtInSession_RedirectsToHomeIndex()
        {
            var accessorMock = BuildHttpContextAccessorMock(token: null);
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)));
            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Index();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_ApiReturnsSuccess_ReturnsViewWithListOfVoziloVM()
        {
            var accessorMock = BuildHttpContextAccessorMock(token: FakeJwtToken);

            var json = @"
                [
                  {
                    ""idVozilo"": 1,
                    ""marka"":   ""Toyota"",
                    ""model"":   ""Corolla"",
                    ""registracija"": ""ZG-1234""
                  },
                  {
                    ""idVozilo"": 2,
                    ""marka"":   ""Honda"",
                    ""model"":   ""Civic"",
                    ""registracija"": ""ST-5678""
                  }
                ]";

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

           
            var result = await controller.Index();

            
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<VoziloVM>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("Toyota", model[0].Marka);
            Assert.Equal("Honda", model[1].Marka);
        }

        [Fact]
        public async Task Index_ApiReturnsError_ShowsErrorView()
        {
            
            var accessorMock = BuildHttpContextAccessorMock();
            var errorContent = "Something went wrong";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(errorContent)
            };

            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            
            var result = await controller.Index();

            
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error", viewResult.ViewName);
        }


        [Fact]
        public void Create_Get_ReturnsEmptyVoziloView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK))); // not used
            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = controller.Create();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Vozilo>(viewResult.Model);
        }


        [Fact]
        public async Task Create_Post_InvalidModelState_ReturnsViewWithModel()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)));
            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            controller.ModelState.AddModelError("Marka", "Required");

            var vm = new Vozilo
            {
                Marka = "", 
                Model = "SomeModel",
                Registracija = "AB-1234",
                PrometnaFile = null
            };

            var result = await controller.Create(vm);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(vm, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Create_Post_ApiReturnsError_ModelStateHasError_AndReturnsView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request from API")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var vm = new Vozilo
            {
                Marka = "Ford",
                Model = "Focus",
                Registracija = "ZG-9999",
                
                PrometnaFile = null
            };

            var result = await controller.Create(vm);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(vm, viewResult.Model);
            var modelErrors = controller.ModelState[string.Empty].Errors;
            Assert.NotEmpty(modelErrors);
            Assert.Contains(modelErrors, e => e.ErrorMessage.Contains("Greška pri spremanju vozila"));
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToIndex()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.Created);
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var content = "fake file content";
            var bytes = Encoding.UTF8.GetBytes(content);
            var ms = new MemoryStream(bytes);
            var formFile = new FormFile(ms, 0, bytes.Length, "Prometna", "prometna.pdf");

            var vm = new Vozilo
            {
                Marka = "Tesla",
                Model = "Model 3",
                Registracija = "RI-4321",
                PrometnaFile = formFile
            };

            var result = await controller.Create(vm);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(VoziloController.Index), redirect.ActionName);
        }


        [Fact]
        public async Task Edit_Get_ApiReturnsSuccess_ReturnsViewWithVoziloVM()
        {
            var accessorMock = BuildHttpContextAccessorMock(token: FakeJwtToken);

            var json = @"
                {
                  ""idVozilo"": 5,
                  ""marka"":   ""BMW"",
                  ""model"":   ""X5"",
                  ""registracija"": ""OS-5555""
                }";

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Edit(5);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VoziloVM>(viewResult.Model);
            Assert.Equal(5, model.Idvozilo);
            Assert.Equal("BMW", model.Marka);
        }

        [Fact]
        public async Task Edit_Get_ApiReturnsNotFound_ShowsErrorView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Edit(99);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error", viewResult.ViewName);
        }


        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)));
            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var vm = new VoziloVM
            {
                Idvozilo = 10,
                Marka = "Audi",
                Model = "A4",
                Registracija = "VP-7777"
            };

            var result = await controller.Edit(11, vm);

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Edit_Post_InvalidModelState_ReturnsViewWithModel()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var httpClient = new HttpClient(new FakeHttpMessageHandler(
                new HttpResponseMessage(HttpStatusCode.OK)));
            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            controller.ModelState.AddModelError("Marka", "Required");

            var vm = new VoziloVM
            {
                Idvozilo = 20,
                Marka = "", 
                Model = "A6",
                Registracija = "ZG-2020"
            };

            var result = await controller.Edit(20, vm);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(vm, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Post_ApiReturnsError_ModelStateHasError_AndReturnsView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad request on update")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var vm = new VoziloVM
            {
                Idvozilo = 30,
                Marka = "Mazda",
                Model = "3",
                Registracija = "DS-3030"
            };

            var result = await controller.Edit(30, vm);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(vm, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
            var errors = controller.ModelState[string.Empty].Errors;
            Assert.Contains(errors, e => e.ErrorMessage.Contains("API greška"));
        }

        [Fact]
        public async Task Edit_Post_ValidModel_RedirectsToIndex()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var vm = new VoziloVM
            {
                Idvozilo = 40,
                Marka = "Kia",
                Model = "Rio",
                Registracija = "SK-4040",
                PrometnaFile = null
            };

            var result = await controller.Edit(40, vm);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(VoziloController.Index), redirect.ActionName);
        }


        [Fact]
        public async Task Delete_Get_ApiReturnsSuccess_ReturnsViewWithVozilo()
        {
            var accessorMock = BuildHttpContextAccessorMock(token: FakeJwtToken);

            var json = @"
                {
                  ""idVozilo"":     55,
                  ""marka"":         ""Mercedes"",
                  ""model"":         ""C-Class"",
                  ""registracija"":  ""ZG-5555""
                }";

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Delete(55);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CARSHARE_WEBAPP.Models.Vozilo>(viewResult.Model);
            Assert.Equal(55, model.Idvozilo);
            Assert.Equal("Mercedes", model.Marka);
        }

        [Fact]
        public async Task Delete_Get_ApiReturnsError_ShowsErrorView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Cannot delete")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Delete(66);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error", viewResult.ViewName);
        }


        [Fact]
        public async Task DeleteConfirmed_ApiReturnsSuccess_RedirectsToIndex()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.DeleteConfirmed(77);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(VoziloController.Index), redirect.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_ApiReturnsError_ShowsErrorView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Failed to delete")
            };
            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.DeleteConfirmed(88);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error", viewResult.ViewName);
        }


        [Fact]
        public async Task Details_ApiReturnsSuccess_ReturnsViewWithVoziloVM()
        {
            var accessorMock = BuildHttpContextAccessorMock(token: FakeJwtToken);

            var json = @"
                {
                  ""idVozilo"": 99,
                  ""marka"":   ""Seat"",
                  ""model"":   ""Ibiza"",
                  ""registracija"": ""PU-9999""
                }";

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Details(99);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VoziloVM>(viewResult.Model);
            Assert.Equal(99, model.Idvozilo);
            Assert.Equal("Seat", model.Marka);
        }

        [Fact]
        public async Task Details_ApiReturnsNotFound_ShowsErrorView()
        {
            var accessorMock = BuildHttpContextAccessorMock();
            var responseMessage = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not Found")
            };

            var httpClient = new HttpClient(new FakeHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", FakeJwtToken);

            var controller = new TestableVoziloController(accessorMock.Object, BuildLogger(), httpClient);

            var result = await controller.Details(123);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error", viewResult.ViewName);
        }
    }
}
