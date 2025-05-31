
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Session;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

namespace CARSHARE_WEBAPP.UnitTests
{

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new Dictionary<string, byte[]>();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable { get; } = true;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key)
        {
            if (_store.ContainsKey(key))
                _store.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _store[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value);
        }
    }

    public class OglasVoziloControllerTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly OglasVoziloController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly TestSession _testSession;

        public OglasVoziloControllerTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };

            _controller = new OglasVoziloController(_httpClient);

            _httpContext = new DefaultHttpContext();
            _testSession = new TestSession();
            var sessionFeature = new SessionFeature { Session = _testSession };
            _httpContext.Features.Set<ISessionFeature>(sessionFeature);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            var tempDataProvider = new Mock<ITempDataProvider>();
            var tempDataDict = new TempDataDictionary(_httpContext, tempDataProvider.Object);
            _controller.TempData = tempDataDict;
        }


        private void VerifyNoOutstandingHttpCalls()
        {

            _handlerMock.VerifyAll();
        }


        [Fact]
        public async Task ReserveVehicle_Get_WhenOglasExists_ReturnsViewWithModel()
        {
            var id = 3;
            var oglasVm = new OglasVoziloVM
            {
                Idoglasvozilo = id,
                DatumPocetkaRezervacije = new DateTime(2025, 01, 01),
                DatumZavrsetkaRezervacije = new DateTime(2025, 01, 10)
            };
            var reservedDatesStringList = new List<string>
            {
                "2025-01-02",
                "2025-01-03"
            };

            var oglasJson = JsonConvert.SerializeObject(oglasVm);
            var reservedDatesJson = JsonConvert.SerializeObject(reservedDatesStringList);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals($"/api/OglasVozilo/DetaljiOglasaVozila/{id}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(oglasJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals($"/api/OglasVozilo/GetReservedDates/{id}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(reservedDatesJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.ReserveVehicle(id);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<VehicleReservationVM>();

            var model = (VehicleReservationVM)viewResult.Model;
            model.OglasVoziloId.Should().Be(3);
            model.DozvoljeniPocetak.Should().Be(oglasVm.DatumPocetkaRezervacije);
            model.DozvoljeniKraj.Should().Be(oglasVm.DatumZavrsetkaRezervacije);

            model.ReservedDates.Should().HaveCount(2);
            model.ReservedDates.Should().Contain(DateTime.Parse("2025-01-02"));
            model.ReservedDates.Should().Contain(DateTime.Parse("2025-01-03"));

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task ReserveVehicle_Get_WhenOglasNotFound_ReturnsNotFound()
        {
            var id = 7;
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals($"/api/OglasVozilo/DetaljiOglasaVozila/{id}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .Verifiable();

            var actionResult = await _controller.ReserveVehicle(id);

            actionResult.Should().BeOfType<NotFoundResult>();

            VerifyNoOutstandingHttpCalls();
        }


        [Fact]
        public async Task ReserveVehicle_Post_WhenModelStateInvalid_ReturnsViewWithSameModel()
        {
            var bogusModel = new VehicleReservationVM
            {
            };

            _controller.ModelState.AddModelError("Error", "Missing required fields");

            var actionResult = await _controller.ReserveVehicle(bogusModel);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(bogusModel);

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ReserveVehicle_Post_WhenNoUserIdInSession_RedirectsToLoginAccount()
        {
            var validModel = new VehicleReservationVM
            {
                OglasVoziloId = 1,
                DatumPocetkaRezervacije = new DateTime(2025, 01, 01),
                DatumZavrsetkaRezervacije = new DateTime(2025, 01, 02)
            };


            var actionResult = await _controller.ReserveVehicle(validModel);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Login");
            redirect.ControllerName.Should().Be("Account");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ReserveVehicle_Post_WhenPostSucceeds_RedirectsToIndexAndSetsTempData()
        {
            var model = new VehicleReservationVM
            {
                OglasVoziloId = 5,
                DatumPocetkaRezervacije = new DateTime(2025, 02, 10),
                DatumZavrsetkaRezervacije = new DateTime(2025, 02, 12)
            };

            _testSession.SetInt32("UserId", 42);


            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/CreateReservation", StringComparison.OrdinalIgnoreCase)

                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.ReserveVehicle(model);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");   
            _controller.TempData.ContainsKey("Success").Should().BeTrue();
            _controller.TempData["Success"].Should().Be("Reservation was successfully made");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task ReserveVehicle_Post_WhenPostFails_AddsModelErrorAndReturnsView()
        {
            var model = new VehicleReservationVM
            {
                OglasVoziloId = 9,
                DatumPocetkaRezervacije = new DateTime(2025, 03, 01),
                DatumZavrsetkaRezervacije = new DateTime(2025, 03, 02)
            };

            _testSession.SetInt32("UserId", 99);

            var errorBody = "Something went wrong";
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/CreateReservation", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(errorBody, Encoding.UTF8, "text/plain")
                })
                .Verifiable();

            var actionResult = await _controller.ReserveVehicle(model);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(model);

            _controller.ModelState.ErrorCount.Should().Be(1);
            var actualError = _controller.ModelState[string.Empty].Errors[0].ErrorMessage;
            actualError.Should().Be($"Rezervacija nije uspjela: {errorBody}");

            VerifyNoOutstandingHttpCalls();
        }



        [Fact]
        public async Task IndexUser_WhenNoJwtInSession_RedirectsToLoginAccount()
        {
            var actionResult = await _controller.IndexUser();
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var r = (RedirectToActionResult)actionResult;
            r.ActionName.Should().Be("Login");
            r.ControllerName.Should().Be("Account");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task IndexUser_WhenJwtButNoUserId_RedirectsToLoginAccount()
        {
            _testSession.SetString("JWToken", "dummy.jwt.token");
            var actionResult = await _controller.IndexUser();
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var r = (RedirectToActionResult)actionResult;
            r.ActionName.Should().Be("Login");
            r.ControllerName.Should().Be("Account");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task IndexUser_WhenApiReturnsNonSuccessCode_ReturnsEmptyListView()
        {
            _testSession.SetString("JWToken", "dummy.jwt.token");
            _testSession.SetInt32("UserId", 55);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetAllByUser?userId=55", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                })
                .Verifiable();

            var actionResult = await _controller.IndexUser();
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeAssignableTo<List<OglasVoziloVM>>();
            var list = (List<OglasVoziloVM>)viewResult.Model;
            list.Should().BeEmpty();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task IndexUser_WhenApiReturnsSuccess_ReturnsViewWithList()
        {
            _testSession.SetString("JWToken", "dummy.jwt.token");
            _testSession.SetInt32("UserId", 55);

            var fakeList = new List<OglasVoziloVM>
            {
                new OglasVoziloVM { Idoglasvozilo = 1, Marka = "BMW", Model = "X5", Username = "driver1" },
                new OglasVoziloVM { Idoglasvozilo = 2, Marka = "Audi", Model = "A4", Username = "driver2" }
            };

            var fakeJson = JsonConvert.SerializeObject(fakeList);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetAllByUser?userId=55", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.IndexUser();
            
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeAssignableTo<List<OglasVoziloVM>>();
            var list = (List<OglasVoziloVM>)viewResult.Model;
            list.Should().HaveCount(2);
            list[0].Marka.Should().Be("BMW");
            list[1].Marka.Should().Be("Audi");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Index_WhenApiReturnsNonSuccessCode_ReturnsEmptyListView()
        {
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetAll", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable
                })
                .Verifiable();

            var actionResult = await _controller.Index();
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeAssignableTo<List<OglasVoziloVM>>();
            var list = (List<OglasVoziloVM>)viewResult.Model;
            list.Should().BeEmpty();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Index_WhenApiReturnsSuccess_ReturnsViewWithList()
        {
            var fakeList = new List<OglasVoziloVM>
            {
                new OglasVoziloVM { Idoglasvozilo = 10, Marka = "Tesla", Model = "Model X" }
            };
            var fakeJson = JsonConvert.SerializeObject(fakeList);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetAll", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Index();
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeAssignableTo<List<OglasVoziloVM>>();
            var list = (List<OglasVoziloVM>)viewResult.Model;
            list.Should().HaveCount(1);
            list[0].Marka.Should().Be("Tesla");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Details_WhenNoJwtInSession_RedirectsToLoginAccount()
        {

            var actionResult = await _controller.Details(15);
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var r = (RedirectToActionResult)actionResult;
            r.ActionName.Should().Be("Login");
            r.ControllerName.Should().Be("Account");
            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Details_WhenApiReturnsNonSuccess_RedirectsOrNotFound()
        {
            _testSession.SetString("JWToken", "abc.def.ghi");

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/DetaljiOglasaVoznje/8", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .Verifiable();

            var actionResult = await _controller.Details(8);
            actionResult.Should().BeOfType<NotFoundResult>();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Details_WhenApiReturnsSuccess_ReturnsViewWithOglasVoziloVM()
        {
            _testSession.SetString("JWToken", "jwt.token.here");
            var vm = new OglasVoziloVM
            {
                Idoglasvozilo = 20,
                Marka = "Ford",
                Model = "Mustang"
            };
            var json = JsonConvert.SerializeObject(vm);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/DetaljiOglasaVoznje/20", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Details(20);
            
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<OglasVoziloVM>();
            var model = (OglasVoziloVM)viewResult.Model;
            model.Marka.Should().Be("Ford");
            model.Model.Should().Be("Mustang");

            VerifyNoOutstandingHttpCalls();
        }


        [Fact]
        public async Task Create_Get_WhenVehicleNotFound_ReturnsNotFoundWithMessage()
        {
            var id = 50;
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/Vozilo/GetVehicleById/50", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .Verifiable();

            var actionResult = await _controller.Create(id);
            actionResult.Should().BeOfType<NotFoundObjectResult>();
            var notFound = (NotFoundObjectResult)actionResult;
            notFound.Value.Should().Be("Vehicle not found.");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Create_Get_WhenVehicleOrDriverNull_ReturnsNotFoundWithMessage()
        {
            var id = 101;
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/Vozilo/GetVehicleById/101", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("null", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Create(id);

            actionResult.Should().BeOfType<NotFoundObjectResult>();
            var notFound = (NotFoundObjectResult)actionResult;
            notFound.Value.Should().Be("Vehicle or Driver data not found.");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Create_Get_WhenVehicleWithDriverExists_ReturnsViewWithOglasVoziloVM()
        {
            var id = 202;
            var fakeVozac = new Korisnik
            {
                Username = "driverX",
                Ime = "Ivan",
                Prezime = "Horvat"
            };
            var fakeVozilo = new Vozilo
            {
                Idvozilo = id,
                Marka = "Opel",
                Model = "Astra",
                Registracija = "ZG1234AB",
                Vozac = fakeVozac
            };

            var json = JsonConvert.SerializeObject(fakeVozilo);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/Vozilo/GetVehicleById/202", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            // Act
            var actionResult = await _controller.Create(id);

            // Assert
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<OglasVoziloVM>();

            var model = (OglasVoziloVM)viewResult.Model;
            model.VoziloId.Should().Be(202);
            model.Username.Should().Be("driverX");
            model.Ime.Should().Be("Ivan");
            model.Prezime.Should().Be("Horvat");
            model.Marka.Should().Be("Opel");
            model.Model.Should().Be("Astra");
            model.Registracija.Should().Be("ZG1234AB");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Create_Post_WhenModelStateInvalid_ReturnsViewWithSameModel()
        {
            var bogus = new OglasVoziloVM(); 
            _controller.ModelState.AddModelError("Error", "invalid data");

            var actionResult = await _controller.Create(bogus);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(bogus);

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_Post_WhenApiReturnsSuccess_RedirectsToIndex()
        {
            var vm = new OglasVoziloVM
            {
                VoziloId = 300,
                DatumPocetkaRezervacije = new DateTime(2025, 05, 01),
                DatumZavrsetkaRezervacije = new DateTime(2025, 05, 05)
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/KreirajOglasVozilo", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("", Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Create(vm);

            
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Create_Post_WhenApiReturnsFailure_AddsModelErrorAndReturnsView()
        {
            var vm = new OglasVoziloVM
            {
                VoziloId = 400,
                DatumPocetkaRezervacije = new DateTime(2025, 06, 01),
                DatumZavrsetkaRezervacije = new DateTime(2025, 06, 03)
            };

            var errorText = "Duplicate entry";
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/KreirajOglasVozilo", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(errorText, Encoding.UTF8, "text/plain")
                })
                .Verifiable();

            var actionResult = await _controller.Create(vm);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(vm);

            _controller.ModelState.ErrorCount.Should().Be(1);
            var actualError = _controller.ModelState[string.Empty].Errors[0].ErrorMessage;
            actualError.Should().Be("Error while creating the advertisement.");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Edit_Get_WhenNotFound_ReturnsNotFound()
        {
            var id = 777;
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetVehicleById/777", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .Verifiable();

            var actionResult = await _controller.Edit(id);
            actionResult.Should().BeOfType<NotFoundResult>();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Edit_Get_WhenVehicleFound_ReturnsViewWithModel()
        {
            // Arrange
            var id = 888;
            var vm = new OglasVoziloVM
            {
                Idoglasvozilo = id,
                Marka = "Toyota",
                Model = "Corolla"
            };
            var json = JsonConvert.SerializeObject(vm);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetVehicleById/888", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Edit(id);
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<OglasVoziloVM>();
            var model = (OglasVoziloVM)viewResult.Model;
            model.Marka.Should().Be("Toyota");
            model.Model.Should().Be("Corolla");

            VerifyNoOutstandingHttpCalls();
        }


        [Fact]
        public async Task Edit_Post_WhenModelStateInvalid_ReturnsViewWithSameModel()
        {
            // Arrange
            var vm = new OglasVoziloVM
            {
                Idoglasvozilo = 999,
                Marka = "", 
                Model = ""
            };
            _controller.ModelState.AddModelError("Error", "invalid");

            var actionResult = await _controller.Edit(999, vm);
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(vm);

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Edit_Post_WhenApiReturnsSuccess_RedirectsToIndex()
        {
            var id = 555;
            var vm = new OglasVoziloVM
            {
                Idoglasvozilo = id,
                Marka = "Kia",
                Model = "Rio"
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Put
                        && req.RequestUri.PathAndQuery.Equals($"/api/OglasVozilo/AzurirajOglasVozilo/{id}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var actionResult = await _controller.Edit(id, vm);
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Edit_Post_WhenApiReturnsFailure_AddsModelErrorAndReturnsView()
        {
            var id = 666;
            var vm = new OglasVoziloVM
            {
                Idoglasvozilo = id,
                Marka = "Renault",
                Model = "Clio"
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Put
                        && req.RequestUri.PathAndQuery.Equals($"/api/OglasVozilo/AzurirajOglasVozilo/{id}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();

            var actionResult = await _controller.Edit(id, vm);
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(vm);

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty].Errors[0].ErrorMessage.Should().Be("Greška pri ažuriranju oglasa.");

            VerifyNoOutstandingHttpCalls();
        }


        [Fact]
        public async Task Delete_Get_WhenNoJwt_RedirectsToLoginKorisnik()
        { 
            var actionResult = await _controller.Delete(12);
            // Assert
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var r = (RedirectToActionResult)actionResult;
            r.ActionName.Should().Be("Login");
            r.ControllerName.Should().Be("Korisnik");
            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_Get_WhenApiReturnsNotFound_ReturnsNotFound()
        {
            _testSession.SetString("JWToken", "token.here");
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetOglasVoziloById/99", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .Verifiable();

            var actionResult = await _controller.Delete(99);
            actionResult.Should().BeOfType<NotFoundResult>();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Delete_Get_WhenApiReturnsSuccess_ReturnsViewWithModel()
        {
            _testSession.SetString("JWToken", "token.here");
            var vm = new OglasVoziloVM
            {
                Idoglasvozilo = 101,
                Marka = "Seat",
                Model = "Ibiza"
            };
            var json = JsonConvert.SerializeObject(vm);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/GetOglasVoziloById/101", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Delete(101);
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<OglasVoziloVM>();
            var model = (OglasVoziloVM)viewResult.Model;
            model.Marka.Should().Be("Seat");
            model.Model.Should().Be("Ibiza");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task DeleteConfirmed_WhenNoJwt_RedirectsToLoginKorisnik()
        {
            var actionResult = await _controller.DeleteConfirmed(123);
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Login");
            redirect.ControllerName.Should().Be("Korisnik");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteConfirmed_WhenApiReturnsFailure_SetsViewBagErrorAndRedirectsIndex()
        {
            _testSession.SetString("JWToken", "token123");

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete
                        && req.RequestUri.PathAndQuery.Equals("/api/ObrisiOglasVozilo/7", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();

    
            var actionResult = await _controller.DeleteConfirmed(7);


            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");

            string errorMessage = _controller.ViewBag.Error as string;
            errorMessage.Should().Be("Failed to delete vehicle.");

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task DeleteConfirmed_WhenApiReturnsSuccess_RedirectsToIndexUser()
        {
            _testSession.SetString("JWToken", "tokenABC");
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Delete
                        && req.RequestUri.PathAndQuery.Equals("/api/ObrisiOglasVozilo/55", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var actionResult = await _controller.DeleteConfirmed(55);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("IndexUser");

            VerifyNoOutstandingHttpCalls();
        }
    }
}
