
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
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
  
   

    public class PorukaVoznjaControllerTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly PorukaVoznjaController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly TestSession _testSession;

        public PorukaVoznjaControllerTests()
        {
            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };

            _controller = new PorukaVoznjaController(_httpClient);

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
        public async Task Index_WhenApiReturnsSuccess_ReturnsViewWithMessages()
        {
            int korVoznjaId = 7;
            int korisnikId = 5;

            var dummyMessages = new List<PorukaVoznjaGetVM>
            {
                new PorukaVoznjaGetVM {},
                new PorukaVoznjaGetVM {}
            };
            string json = JsonConvert.SerializeObject(dummyMessages);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.PathAndQuery.Equals($"/api/Poruka/GetMessagesForRide?korisnikVoznjaId={korVoznjaId}", StringComparison.OrdinalIgnoreCase)
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
            var result = await _controller.Index(korVoznjaId, korisnikId);

            result.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)result;
            viewResult.Model.Should().BeOfType<PorukaVoznjaSendVM>();

            var vm = (PorukaVoznjaSendVM)viewResult.Model;
            vm.Korisnikvoznjaid.Should().Be(korVoznjaId);
            vm.VozacId.Should().Be(korisnikId);
            vm.PutnikId.Should().BeNull();
            vm.Messages.Should().HaveCount(2);

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Index_WhenApiReturnsFailure_ReturnsViewWithEmptyMessages()
        {
            int korVoznjaId = 42;
            int korisnikId = 9;

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.PathAndQuery.Equals($"/api/Poruka/GetMessagesForRide?korisnikVoznjaId={korVoznjaId}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();

            var result = await _controller.Index(korVoznjaId, korisnikId);

            result.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)result;
            viewResult.Model.Should().BeOfType<PorukaVoznjaSendVM>();

            var vm = (PorukaVoznjaSendVM)viewResult.Model;
            vm.Korisnikvoznjaid.Should().Be(korVoznjaId);
            vm.VozacId.Should().Be(korisnikId);
            vm.PutnikId.Should().BeNull();
            vm.Messages.Should().BeEmpty();

            VerifyNoOutstandingHttpCalls();
        }


        [Fact]
        public async Task SendMessage_WhenMessageIsEmpty_AddsModelErrorAndRedirectsIndex()
        {
            // Arrange
            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 100,
                PutnikId = null,
                VozacId = 10,
                Message = "   " 
            };

            var result = await _controller.SendMessage(vm);

            result.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)result;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["KorisnikVoznjaId"].Should().Be(vm.Korisnikvoznjaid);
            redirect.RouteValues["PutnikId"].Should().Be(vm.PutnikId);
            redirect.RouteValues["VozacId"].Should().Be(vm.VozacId);

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty].Errors[0].ErrorMessage
                .Should().Be("Message cannot be empty.");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendMessage_WhenNoClaims_ReturnsUnauthorized()
        {
            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 200,
                PutnikId = null,
                VozacId = null,
                Message = "Hi"
            };

            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var result = await _controller.SendMessage(vm);

            result.Should().BeOfType<UnauthorizedResult>();
            _controller.ModelState.ErrorCount.Should().Be(0);

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendMessage_WhenInvalidRole_ReturnsBadRequest()
        {
            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 300,
                PutnikId = null,
                VozacId = null,
                Message = "Test"
            };

            var claims = new[]
            {
                new Claim("sub", "55"),
                new Claim("role", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            var result = await _controller.SendMessage(vm);

            result.Should().BeOfType<BadRequestObjectResult>();
            var badReq = (BadRequestObjectResult)result;
            badReq.Value.Should().Be("User role is not valid for sending messages.");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendMessage_WhenDriverRoleAndPostSucceeds_RedirectsIndexWithCorrectRouteValues()
        {
            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 400,
                PutnikId = null,
                VozacId = null,
                Message = "Driver says hi"
            };

            var claims = new[]
            {
                new Claim("sub", "99"),
                new Claim("role", "DRIVER")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.PathAndQuery.Equals("/api/Poruka/SendMessageForRide", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var result = await _controller.SendMessage(vm);

            result.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)result;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["KorisnikVoznjaId"].Should().Be(vm.Korisnikvoznjaid);
            redirect.RouteValues["PutnikId"].Should().BeNull();
            redirect.RouteValues["VozacId"].Should().Be(99);

            _controller.ModelState.ErrorCount.Should().Be(0);

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task SendMessage_WhenPassengerRoleAndPostSucceeds_RedirectsIndexWithCorrectRouteValues()
        {
            
            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 500,
                PutnikId = null,
                VozacId = null,
                Message = "Passenger here"
            };

            var claims = new[]
            {
                new Claim("sub", "1234"),
                new Claim("role", "PASSENGER")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.PathAndQuery.Equals("/api/Poruka/SendMessageForRide", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            // Act
            var result = await _controller.SendMessage(vm);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)result;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["KorisnikVoznjaId"].Should().Be(vm.Korisnikvoznjaid);
            redirect.RouteValues["PutnikId"].Should().Be(1234);
            redirect.RouteValues["VozacId"].Should().BeNull();

            _controller.ModelState.ErrorCount.Should().Be(0);

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task SendMessage_WhenPostFails_AddsModelErrorAndRedirectsIndex()
        {
            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 600,
                PutnikId = null,
                VozacId = null,
                Message = "This should fail"
            };

            var claims = new[]
            {
                new Claim("sub", "321"),
                new Claim("role", "DRIVER")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.PathAndQuery.Equals("/api/Poruka/SendMessageForRide", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();

            var result = await _controller.SendMessage(vm);

            result.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)result;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["KorisnikVoznjaId"].Should().Be(vm.Korisnikvoznjaid);
            redirect.RouteValues["PutnikId"].Should().BeNull();
            redirect.RouteValues["VozacId"].Should().Be(321);

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty].Errors[0].ErrorMessage
                .Should().Be("Failed to send message.");

            VerifyNoOutstandingHttpCalls();
        }
    }
}
