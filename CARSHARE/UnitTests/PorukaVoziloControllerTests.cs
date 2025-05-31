
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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


    public class PorukaVoziloControllerTests
    {
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly PorukaVoziloController _controller;
        private readonly DefaultHttpContext _httpContext;
        private readonly TestSession _testSession;

        public PorukaVoziloControllerTests()
        {

            _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

         
            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };

            _controller = new PorukaVoziloController(_httpClient);

      
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
            var korVozId = 7;
            var putnikId = 3;
            var vozacId = 5;

            var dummyMessages = new List<PorukaGetVM>
            {
                new PorukaGetVM {},
                new PorukaGetVM {}
            };
            var json = JsonConvert.SerializeObject(dummyMessages);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals($"/api/Poruka/GetMessagesForRide?korisnikVoziloId={korVozId}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                })
                .Verifiable();

            var actionResult = await _controller.Index(korVozId, putnikId, vozacId);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<PorukaVoziloVM>();

            var model = (PorukaVoziloVM)viewResult.Model;
            model.Korisnikvoziloid.Should().Be(korVozId);
            model.PutnikId.Should().Be(putnikId);
            model.VozacId.Should().Be(vozacId);

            model.Messages.Should().HaveCount(2);

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task Index_WhenApiReturnsNonSuccess_ReturnsViewWithEmptyMessages()
        {
            // Arrange
            var korVozId = 42;
            int? putnikId = null;
            int? vozacId = 11;

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri.PathAndQuery.Equals($"/api/Poruka/GetMessagesForRide?korisnikVoziloId={korVozId}", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();

            var actionResult = await _controller.Index(korVozId, putnikId, vozacId);

            actionResult.Should().BeOfType<ViewResult>();
            var viewResult = (ViewResult)actionResult;
            viewResult.Model.Should().BeOfType<PorukaVoziloVM>();

            var model = (PorukaVoziloVM)viewResult.Model;
            model.Korisnikvoziloid.Should().Be(korVozId);
            model.PutnikId.Should().Be(putnikId);
            model.VozacId.Should().Be(vozacId);

            model.Messages.Should().BeEmpty();

            VerifyNoOutstandingHttpCalls();
        }



        [Fact]
        public async Task SendMessage_Post_WhenMessageIsEmpty_AddsModelErrorAndRedirectsIndex()
        {
            // Arrange
            var vm = new PorukaVoziloVM
            {
                Korisnikvoziloid = 10,
                PutnikId = 2,
                VozacId = 5,
                Message = "   " 
            };

            
            var actionResult = await _controller.SendMessage(vm);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["korisnikVoziloId"].Should().Be(vm.Korisnikvoziloid);
            redirect.RouteValues["putnikId"].Should().Be(vm.PutnikId);
            redirect.RouteValues["vozacId"].Should().Be(vm.VozacId);

            _controller.ModelState.ErrorCount.Should().Be(1);
            var actualError = _controller.ModelState[string.Empty].Errors[0].ErrorMessage;
            actualError.Should().Be("Message cannot be empty.");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendMessage_Post_WhenNoClaims_ReturnsUnauthorized()
        {
            // Arrange
            var vm = new PorukaVoziloVM
            {
                Korisnikvoziloid = 100,
                PutnikId = null,
                VozacId = null,
                Message = "Hello!"
            };

            _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var actionResult = await _controller.SendMessage(vm);

            actionResult.Should().BeOfType<UnauthorizedResult>();

            _controller.ModelState.ErrorCount.Should().Be(0);

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendMessage_Post_WhenInvalidRole_ReturnsBadRequest()
        {
            var vm = new PorukaVoziloVM
            {
                Korisnikvoziloid = 20,
                PutnikId = null,
                VozacId = null,
                Message = "Test"
            };

            var claims = new[]
            {
                new Claim("sub", "42"),
                new Claim("role", "ADMIN")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            var actionResult = await _controller.SendMessage(vm);

            actionResult.Should().BeOfType<BadRequestObjectResult>();
            var badReq = (BadRequestObjectResult)actionResult;
            badReq.Value.Should().Be("User role is not valid for sending messages.");

            _handlerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SendMessage_Post_WhenDriverRoleAndPostSucceeds_RedirectsIndexWithCorrectRouteValues()
        {
            var vm = new PorukaVoziloVM
            {
                Korisnikvoziloid = 300,
                PutnikId = null,
                VozacId = null,
                Message = "Hi from driver!"
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
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/Poruka/SendMessageForRide", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var actionResult = await _controller.SendMessage(vm);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["korisnikVoziloId"].Should().Be(vm.Korisnikvoziloid);
            redirect.RouteValues["putnikId"].Should().BeNull();
            redirect.RouteValues["vozacId"].Should().Be(99);

            _controller.ModelState.ErrorCount.Should().Be(0);

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task SendMessage_Post_WhenPassengerRoleAndPostSucceeds_RedirectsIndexWithCorrectRouteValues()
        {
            var vm = new PorukaVoziloVM
            {
                Korisnikvoziloid = 400,
                PutnikId = null,
                VozacId = null,
                Message = "Hi from passenger!"
            };

            var claims = new[]
            {
                new Claim("sub", "123"),
                new Claim("role", "PASSENGER")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/Poruka/SendMessageForRide", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                })
                .Verifiable();

            var actionResult = await _controller.SendMessage(vm);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["korisnikVoziloId"].Should().Be(vm.Korisnikvoziloid);
            redirect.RouteValues["putnikId"].Should().Be(123);
            redirect.RouteValues["vozacId"].Should().BeNull();

            _controller.ModelState.ErrorCount.Should().Be(0);

            VerifyNoOutstandingHttpCalls();
        }

        [Fact]
        public async Task SendMessage_Post_WhenPostFails_AddsModelErrorAndRedirectsIndex()
        {
            var vm = new PorukaVoziloVM
            {
                Korisnikvoziloid = 555,
                PutnikId = null,
                VozacId = null,
                Message = "This will fail"
            };

            var claims = new[]
            {
                new Claim("sub", "777"),
                new Claim("role", "DRIVER")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri.PathAndQuery.Equals("/api/Poruka/SendMessageForRide", StringComparison.OrdinalIgnoreCase)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                })
                .Verifiable();

            var actionResult = await _controller.SendMessage(vm);

            actionResult.Should().BeOfType<RedirectToActionResult>();
            var redirect = (RedirectToActionResult)actionResult;
            redirect.ActionName.Should().Be("Index");
            redirect.RouteValues["korisnikVoziloId"].Should().Be(vm.Korisnikvoziloid);
            redirect.RouteValues["putnikId"].Should().BeNull();
            redirect.RouteValues["vozacId"].Should().Be(777);
            _controller.ModelState.ErrorCount.Should().Be(1);
            var actualError = _controller.ModelState[string.Empty].Errors[0].ErrorMessage;
            actualError.Should().Be("Failed to send message.");

            VerifyNoOutstandingHttpCalls();
        }
    }
}
