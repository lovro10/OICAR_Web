using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace CARSHARE_WEBAPP.Tests.Controllers
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            _handlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handlerFunc(request, cancellationToken);
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _storage = new();
        public IEnumerable<string> Keys => _storage.Keys;
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable { get; } = true;

        public void Clear() => _storage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _storage.Remove(key);
        public void Set(string key, byte[] value) => _storage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _storage.TryGetValue(key, out value);
    }

    public class OglasVoziloControllerTests
    {
        private OglasVoziloController CreateController(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc,
            out TestSession session,
            out ITempDataDictionary tempData)
        {
            var handler = new FakeHttpMessageHandler(handlerFunc);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };

            var controller = new OglasVoziloController(client);

            var httpContext = new DefaultHttpContext();
            session = new TestSession();
            httpContext.Session = session;
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task ReservationDetails_UserNotLoggedIn_RedirectsToLogin()
        {
            var controller = CreateController(
                (req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var session,
                out _);

            var result = await controller.ReservationDetails(123);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task ReservationDetails_DetailApiFails_ReturnsNotFound()
        {
            var controller = CreateController(
                (req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
                out var session,
                out _);

            session.SetInt32("UserId", 7);

            var result = await controller.ReservationDetails(55);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ReservationDetails_AllApisSucceed_ReturnsViewWithModel()
        {
            var oglasVm = new OglasVoziloVM
            {
                Idoglasvozilo = 42,
                DatumPocetkaRezervacije = new DateTime(2025, 6, 1),
                DatumZavrsetkaRezervacije = new DateTime(2025, 6, 30)
            };
            var reserved = new List<string> { "2025-06-10", "2025-06-11" };

            var controller = CreateController(async (req, ct) =>
            {
                if (req.RequestUri.PathAndQuery.Contains("DetaljiOglasaVozila"))
                {
                    var json = JsonConvert.SerializeObject(oglasVm);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    var json = JsonConvert.SerializeObject(reserved);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
            },
            out var session,
            out _);

            session.SetInt32("UserId", 99);

            var result = await controller.ReservationDetails(42);
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<VehicleReservationVM>(view.Model);

            Assert.Equal(42, model.OglasVoziloId);
            Assert.Equal(new DateTime(2025, 6, 1), model.DozvoljeniPocetak);
            Assert.Equal(new DateTime(2025, 6, 30), model.DozvoljeniKraj);
            Assert.Collection(model.ReservedDates,
                d => Assert.Equal(new DateTime(2025, 6, 10), d),
                d => Assert.Equal(new DateTime(2025, 6, 11), d));
        }

        [Fact]
        public async Task ReserveVehicle_Post_InvalidModel_ReturnsSameView()
        {
            var controller = CreateController(
                (req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var session,
                out _);

            session.SetInt32("UserId", 5);

            controller.ModelState.AddModelError("DatumPocetkaRezervacije", "Required");

            var vm = new VehicleReservationVM();
            var result = await controller.ReserveVehicle(vm);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Same(vm, view.Model);
        }

        [Fact]
        public async Task ReserveVehicle_Post_SuccessfulReservation_RedirectsToIndexAndSetsTempData()
        {
            var controller = CreateController(
                (req, ct) =>
                {
                    if (req.Method == HttpMethod.Post && req.RequestUri.PathAndQuery.Contains("CreateReservation"))
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]", Encoding.UTF8, "application/json")
                    });
                },
                out var session,
                out var tempData);

            session.SetInt32("UserId", 11);

            var vm = new VehicleReservationVM
            {
                OglasVoziloId = 77,
                DatumPocetkaRezervacije = new DateTime(2025, 7, 1),
                DatumZavrsetkaRezervacije = new DateTime(2025, 7, 5)
            };

            var result = await controller.ReserveVehicle(vm);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(tempData.ContainsKey("Success"));
            Assert.Equal("Reservation was successfully made", tempData["Success"]);
        }

        [Fact]
        public async Task ReserveVehicle_Post_ApiFails_ReturnsViewWithError()
        {
            var errorText = "Full!";
            var controller = CreateController(
                (req, ct) =>
                {
                    if (req.Method == HttpMethod.Post)
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(errorText)
                        });
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]", Encoding.UTF8, "application/json")
                    });
                },
                out var session,
                out _);

            session.SetInt32("UserId", 15);

            var vm = new VehicleReservationVM
            {
                OglasVoziloId = 99,
                DatumPocetkaRezervacije = DateTime.Today.AddDays(1),
                DatumZavrsetkaRezervacije = DateTime.Today.AddDays(2)
            };

            var result = await controller.ReserveVehicle(vm);
            var view = Assert.IsType<ViewResult>(result);

            Assert.False(controller.ModelState.IsValid);
            Assert.Contains(controller.ModelState[string.Empty].Errors, e => e.ErrorMessage.Contains(errorText));
            Assert.Same(vm, view.Model);
        }
    }
}
