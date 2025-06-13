using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Xunit;

namespace CARSHARE_WEBAPP.Tests.Controllers
{

    public class PorukaVoziloControllerTests
    {
        private PorukaVoziloController CreateController(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc,
            out DefaultHttpContext httpContext)
        {
            var handler = new FakeHttpMessageHandler(handlerFunc);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            var controller = new PorukaVoziloController(client);
            httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            return controller;
        }

        [Fact]
        public async Task Index_ApiFails_ReturnsEmptyMessages()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
                out var ctx);

            var result = await ctrl.Index(korisnikVoziloId: 5, putnikId: null, vozacId: null);
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PorukaVoziloSendVM>(view.Model);
            Assert.Equal(5, model.Korisnikvoziloid);
            Assert.Null(model.PutnikId);
            Assert.Null(model.VozacId);
            Assert.Empty(model.Messages);
        }

        [Fact]
        public async Task Index_ApiSucceeds_ReturnsMessages()
        {
            var msgs = new List<PorukaVoziloGetVM>
            {
                new PorukaVoziloGetVM { Content = "Hello" }
            };
            var json = JsonConvert.SerializeObject(msgs);

            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }),
                out var ctx);

            var result = await ctrl.Index(7, putnikId: 2, vozacId: 3);
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PorukaVoziloSendVM>(view.Model);
            Assert.Equal(7, model.Korisnikvoziloid);
            Assert.Equal(2, model.PutnikId);
            Assert.Equal(3, model.VozacId);
            Assert.Single(model.Messages);
            Assert.Equal("Hello", model.Messages.First().Content);
        }

        [Fact]
        public async Task SendMessage_EmptyMessage_RedirectsBackWithError()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx);

            var vm = new PorukaVoziloSendVM
            {
                Korisnikvoziloid = 10,
                PutnikId = 4,
                VozacId = 5,
                Message = "   "
            };

            var result = await ctrl.SendMessage(vm);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(10, redirect.RouteValues["korisnikVoziloId"]);
            Assert.Equal(4, redirect.RouteValues["putnikId"]);
            Assert.Equal(5, redirect.RouteValues["vozacId"]);
            Assert.True(!ctrl.ModelState.IsValid);
            Assert.Contains(ctrl.ModelState[string.Empty].Errors, e => e.ErrorMessage == "Message cannot be empty.");
        }

        [Fact]
        public async Task SendMessage_NoUserClaims_ReturnsUnauthorized()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx);

            ctx.User = new ClaimsPrincipal(new ClaimsIdentity()); // no claims

            var vm = new PorukaVoziloSendVM { Message = "Hi" };
            var result = await ctrl.SendMessage(vm);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task SendMessage_InvalidRole_ReturnsBadRequest()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim("sub", "8"),
                new Claim("role", "ADMIN")
            }, "test");
            ctx.User = new ClaimsPrincipal(identity);

            var vm = new PorukaVoziloSendVM { Message = "Hello" };
            var result = await ctrl.SendMessage(vm);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User role is not valid for sending messages.", bad.Value);
        }

        [Theory]
        [InlineData("DRIVER", "vozacId")]
        [InlineData("PASSENGER", "putnikId")]
        public async Task SendMessage_ValidRole_CallsApiAndRedirects(string role, string routeKey)
        {
            var called = false;
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post && req.RequestUri.PathAndQuery.Contains("SendMessageForVehicle"))
                {
                    called = true;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, out var ctx);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim("sub", "12"),
                new Claim("role", role)
            }, "test");
            ctx.User = new ClaimsPrincipal(identity);

            var vm = new PorukaVoziloSendVM
            {
                Korisnikvoziloid = 20,
                Message = "Test"
            };

            var result = await ctrl.SendMessage(vm);
            Assert.True(called);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(20, redirect.RouteValues["korisnikVoziloId"]);
            Assert.Contains(routeKey, redirect.RouteValues.Keys);
            Assert.Equal(12, redirect.RouteValues[routeKey]);
        }

        [Fact]
        public async Task SendMessage_ApiFails_AddsModelErrorAndRedirects()
        {
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, out var ctx);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim("sub", "15"),
                new Claim("role", "PASSENGER")
            }, "test");
            ctx.User = new ClaimsPrincipal(identity);

            var vm = new PorukaVoziloSendVM
            {
                Korisnikvoziloid = 30,
                Message = "Hi"
            };

            var result = await ctrl.SendMessage(vm);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(!ctrl.ModelState.IsValid);
            Assert.Contains(ctrl.ModelState[string.Empty].Errors, e => e.ErrorMessage == "Failed to send message.");
        }
    }
}
