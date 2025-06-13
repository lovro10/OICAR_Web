using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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

    public class PorukaVoznjaControllerTests
    {
        PorukaVoznjaController CreateController(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc,
            out DefaultHttpContext ctx,
            out TestSession session)
        {
            var handler = new FakeHttpMessageHandler(handlerFunc);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5194/api/")
            };
            var controller = new PorukaVoznjaController(client);
            ctx = new DefaultHttpContext();
            session = new TestSession();
            ctx.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };
            return controller;
        }

        [Fact]
        public async Task Index_ApiFails_ReturnsEmptyMessages()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
                out var ctx, out var sess);

            var result = await ctrl.Index(korisnikVoznjaId: 5, korisnikId: 2);
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PorukaVoznjaSendVM>(view.Model);

            Assert.Equal(5, model.Korisnikvoznjaid);
            Assert.Null(model.PutnikId);
            Assert.Equal(2, model.VozacId);
            Assert.Empty(model.Messages);
        }

        [Fact]
        public async Task Index_ApiSucceeds_ReturnsMessages()
        {
            var msgs = new List<PorukaVoznjaGetVM> { new() { Content = "Test" } };
            var json = JsonConvert.SerializeObject(msgs);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }),
                out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.Index(7, korisnikId: 3));
            var model = Assert.IsType<PorukaVoznjaSendVM>(view.Model);

            Assert.Equal(7, model.Korisnikvoznjaid);
            Assert.Null(model.PutnikId);
            Assert.Equal(3, model.VozacId);
            Assert.Single(model.Messages);
            Assert.Equal("Test", model.Messages.First().Content);
        }

        [Fact]
        public async Task Join_ApiSucceeds_SetsPutnikId()
        {
            var msgs = new List<PorukaVoznjaGetVM> { new() { Content = "Hi" } };
            var json = JsonConvert.SerializeObject(msgs);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }),
                out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.Join(9, korisnikId: 4));
            var model = Assert.IsType<PorukaVoznjaSendVM>(view.Model);

            Assert.Equal(9, model.Korisnikvoznjaid);
            Assert.Equal(4, model.PutnikId);
            Assert.Null(model.VozacId);
            Assert.Single(model.Messages);
        }

        [Fact]
        public async Task SendMessage_EmptyMessage_RedirectsWithError()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);

            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 11,
                PutnikId = null,
                VozacId = null,
                Message = "   "
            };

            var result = await ctrl.SendMessage(vm);
            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(11, redirect.RouteValues["KorisnikVoznjaId"]);
            Assert.True(!ctrl.ModelState.IsValid);
            Assert.Contains(ctrl.ModelState[string.Empty].Errors,
                e => e.ErrorMessage == "Message cannot be empty.");
        }

        [Fact]
        public async Task SendMessage_NoSession_RedirectsToLogin()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);

            var vm = new PorukaVoznjaSendVM { Message = "Hello" };
            var result = await ctrl.SendMessage(vm);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Auth", redirect.ControllerName);
        }

        [Fact]
        public async Task SendMessage_InvalidRole_ReturnsBadRequest()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);

            sess.SetInt32("UserId", 5);
            sess.SetString("Role", "ADMIN");

            var vm = new PorukaVoznjaSendVM { Korisnikvoznjaid = 12, Message = "Hey" };
            var result = await ctrl.SendMessage(vm);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User role is not valid for sending messages.", bad.Value);
        }

        [Theory]
        [InlineData("DRIVER", "VozacId")]
        [InlineData("PASSENGER", "PutnikId")]
        public async Task SendMessage_ValidRole_CallsApiAndRedirects(string role, string routeKey)
        {
            var called = false;
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post && req.RequestUri.PathAndQuery.Contains("SendMessageForRide"))
                {
                    called = true;
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            out var ctx, out var sess);

            sess.SetInt32("UserId", 8);
            sess.SetString("Role", role);

            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 13,
                Message = "Msg"
            };

            var redirect = Assert.IsType<RedirectToActionResult>(await ctrl.SendMessage(vm));
            Assert.True(called);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal(13, redirect.RouteValues["KorisnikVoznjaId"]);
            Assert.Equal(8, redirect.RouteValues[routeKey]);
        }

        [Fact]
        public async Task SendMessage_ApiFails_AddsModelError()
        {
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            },
            out var ctx, out var sess);

            sess.SetInt32("UserId", 9);
            sess.SetString("Role", "PASSENGER");

            var vm = new PorukaVoznjaSendVM
            {
                Korisnikvoznjaid = 14,
                Message = "Hi"
            };

            var redirect = Assert.IsType<RedirectToActionResult>(await ctrl.SendMessage(vm));
            Assert.False(ctrl.ModelState.IsValid);
            Assert.Contains(ctrl.ModelState[string.Empty].Errors,
                e => e.ErrorMessage == "Failed to send message.");
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}
