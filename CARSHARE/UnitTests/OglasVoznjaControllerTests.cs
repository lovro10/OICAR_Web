
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

   
    public class OglasVoznjaControllerTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IHttpClientFactory> _factoryMock;
        private readonly OglasVoznjaController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly TestSession _testSession;

        public OglasVoznjaControllerTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };

            _factoryMock = new Mock<IHttpClientFactory>(MockBehavior.Strict);
            _factoryMock
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(_httpClient);

            _controller = new OglasVoznjaController(_factoryMock.Object);


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
            _factoryMock.VerifyAll();
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
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVoznja/GetAll", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();


            var actionResult = await _controller.Index();

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeAssignableTo<List<OglasVoznjaVM>>();
            ((List<OglasVoznjaVM>)viewResult.Model).Should().BeEmpty();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Index_WhenApiReturnsSuccess_ReturnsViewWithList()
        {

            var fakeList = new List<OglasVoznjaVM>
            {
                new OglasVoznjaVM { IdOglasVoznja = 1, Odrediste = "Zagreb", Polaziste = "Split" },
                new OglasVoznjaVM { IdOglasVoznja = 2, Odrediste = "Rijeka", Polaziste = "Zadar" }
            };
            var fakeJson = JsonConvert.SerializeObject(fakeList);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVoznja/GetAll", StringComparison.OrdinalIgnoreCase)
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
            viewResult.Model.Should().BeAssignableTo<List<OglasVoznjaVM>>();
            var list = (List<OglasVoznjaVM>)viewResult.Model;
            list.Should().HaveCount(2);
            list[0].Odrediste.Should().Be("Zagreb");
            list[1].Polaziste.Should().Be("Zadar");

            VerifyNoOutstandingHttpCalls();
        }



        [Fact]
        public async Task IndexUser_WhenNoUserIdInSession_RedirectsToLoginAuth()
        {

            var actionResult = await _controller.IndexUser();


            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Login");
            redirect.ControllerName.Should().Be("Auth");

            _handlerMock.VerifyNoOtherCalls();
            _factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task IndexUser_WhenApiReturnsNonSuccessCode_ReturnsEmptyListView()
        {

            _testSession.SetInt32("UserId", 42);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVoznja/GetAllByUser?userId=42", StringComparison.OrdinalIgnoreCase)
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
            viewResult.Model.Should().BeAssignableTo<List<OglasVoznjaVM>>();
            ((List<OglasVoznjaVM>)viewResult.Model).Should().BeEmpty();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task IndexUser_WhenApiReturnsSuccess_PopulatesIsUserInRide()
        {
            var userId = 55;
            _testSession.SetInt32("UserId", userId);

            var fakeList = new List<OglasVoznjaVM>
            {
                new OglasVoznjaVM { IdOglasVoznja = 100, Odrediste = "Osijek", Polaziste = "Varaždin" },
                new OglasVoznjaVM { IdOglasVoznja = 200, Odrediste = "Pula", Polaziste = "Rijeka" }
            };
            var fakeJson = JsonConvert.SerializeObject(fakeList);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVoznja/GetAllByUser?userId=55", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();


            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/KorisnikVoznja/UserJoinedRide?userId=55&oglasVoznjaId=100", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("true", Encoding.UTF8, "text/plain")
                })
                .Verifiable();


            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/KorisnikVoznja/UserJoinedRide?userId=55&oglasVoznjaId=200", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("false", Encoding.UTF8, "text/plain")
                })
                .Verifiable();

      
            var actionResult = await _controller.IndexUser();

         
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeAssignableTo<List<OglasVoznjaVM>>();

            var list = (List<OglasVoznjaVM>)viewResult.Model;
            list.Should().HaveCount(2);

            list[0].IsUserInRide.Should().BeTrue();   
            list[1].IsUserInRide.Should().BeFalse();  

            VerifyNoOutstandingHttpCalls();
        }



        [Fact]
        public async Task Create_Get_WhenVehicleNotFound_ReturnsNotFoundWithMessage()
        {
            var id = 77;
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/Vozilo/GetVehicleById/77", StringComparison.OrdinalIgnoreCase)
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
            var id = 123;
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/Vozilo/GetVehicleById/123", StringComparison.OrdinalIgnoreCase)
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
        public async Task Create_Get_WhenVehicleWithDriverExists_ReturnsViewWithModelAndCities()
        {
            var id = 555;
            var fakeVozac = new Korisnik
            {
                Username = "driverABC",
                Ime = "Marko",
                Prezime = "Marković"
            };
            var fakeVozilo = new Vozilo
            {
                Idvozilo = id,
                Marka = "Fiat",
                Model = "500",
                Registracija = "ZG1111TT",
                Vozac = fakeVozac
            };
            var voziloJson = JsonConvert.SerializeObject(fakeVozilo);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/Vozilo/GetVehicleById/555", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(voziloJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var fakeCities = new List<string> { "Zagreb", "Split", "Rijeka" };
            var citiesJson = JsonConvert.SerializeObject(fakeCities);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/CitySearch", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(citiesJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Create(id);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;

            viewResult.Model.Should().BeOfType<OglasVoznjaVM>();
            var model = (OglasVoznjaVM)viewResult.Model;

            model.VoziloId.Should().Be(555);
            model.Username.Should().Be("driverABC");
            model.Ime.Should().Be("Marko");
            model.Prezime.Should().Be("Marković");
            model.Marka.Should().Be("Fiat");
            model.Model.Should().Be("500");
            model.Registracija.Should().Be("ZG1111TT");


            var citiesFromBag = _controller.ViewBag.Cities as List<string>;
            citiesFromBag.Should().NotBeNull();
            citiesFromBag.Should().HaveCount(3);
            citiesFromBag.Should().Contain(new[] { "Zagreb", "Split", "Rijeka" });

            VerifyNoOutstandingHttpCalls();
        }


        [Fact]
        public async Task Create_Post_WhenModelStateInvalid_ReturnsViewWithSameModel()
        {
            // Arrange
            var bogus = new OglasVoznjaVM {};
            _controller.ModelState.AddModelError("Error", "Invalid data");

            // Act
            var actionResult = await _controller.Create(bogus);

            // Assert
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(bogus);

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_Post_WhenNoUserIdInSession_AddsModelErrorAndReturnsView()
        {
            var vm = new OglasVoznjaVM
            {
                Odrediste = "Zagreb",
                Polaziste = "Split"
            };

            var actionResult = await _controller.Create(vm);

          
            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(vm);

            _controller.ModelState.ErrorCount.Should().Be(1);
            var actualError = _controller.ModelState[string.Empty].Errors[0].ErrorMessage;
            actualError.Should().Be("User must be logged in to create a ride.");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_Post_WhenApiReturnsSuccess_RedirectsToIndex()
        {
            var vm = new OglasVoznjaVM
            {
                Odrediste = "Split",
                Polaziste = "Rijeka",
                DatumIVrijemePolaska = DateTime.Today.AddDays(1)
            };
            _testSession.SetInt32("UserId", 99);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVoznja/KreirajOglasVoznje", StringComparison.OrdinalIgnoreCase)
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
            var vm = new OglasVoznjaVM
            {
                Odrediste = "Zagreb",
                Polaziste = "Osijek",
                DatumIVrijemePolaska = DateTime.Today.AddDays(5)
            };
            _testSession.SetInt32("UserId", 777);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVoznja/KreirajOglasVoznje", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Something bad happened", Encoding.UTF8, "text/plain")
                })
                .Verifiable();

            var actionResult = await _controller.Create(vm);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeSameAs(vm);

            _controller.ModelState.ErrorCount.Should().Be(1);
            var actualError = _controller.ModelState[string.Empty].Errors[0].ErrorMessage;
            actualError.Should().Be("Error while creating the ride advertisement.");

            VerifyNoOutstandingHttpCalls();
        }



        [Fact]
        public async Task Details_WhenNoJwtInSession_RedirectsToLoginAccount()
        {
      
            var actionResult = await _controller.Details(1234);

            // Assert
            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Login");
            redirect.ControllerName.Should().Be("Account");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Details_WhenApiReturnsNonSuccess_ReturnsNotFound()
        {
            _testSession.SetString("JWToken", "header.payload.signature");

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/DetaljiOglasaVoznje/999", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound
                })
                .Verifiable();

            // Act
            var actionResult = await _controller.Details(999);

            actionResult.Should().BeOfType<NotFoundResult>();

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Details_WhenApiReturnsSuccess_ReturnsViewWithModel()
        {
            _testSession.SetString("JWToken", "header.payload.sig");

            var fakeDto = new OglasVoziloVM
            {
                Idoglasvozilo = 5000,
                Marka = "Volvo",
                Model = "V60"
            };
            var fakeJson = JsonConvert.SerializeObject(fakeDto);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals("/api/OglasVozilo/DetaljiOglasaVoznje/5000", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Details(5000);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<OglasVoziloVM>();
            var dto = (OglasVoziloVM)viewResult.Model;
            dto.Marka.Should().Be("Volvo");
            dto.Model.Should().Be("V60");

            VerifyNoOutstandingHttpCalls();
        }
    }
}
