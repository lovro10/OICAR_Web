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
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace CARSHARE_WEBAPP.Tests.Controllers
{


    public class OglasVoznjaControllerTests
    {
        private OglasVoznjaController CreateController(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc,
            out TestSession session,
            out ITempDataDictionary tempData)
        {
            var handler = new FakeHttpMessageHandler(handlerFunc);
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5194/api/") };
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var controller = new OglasVoznjaController(factory.Object);

            var httpContext = new DefaultHttpContext();
            session = new TestSession();
            httpContext.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.TempData = tempData;

            return controller;
        }

        [Fact]
        public async Task Index_ApiFails_ReturnsEmptyListView()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)),
                out var sess, out _);

            var result = await ctrl.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<OglasVoznjaVM>>(view.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_ApiSucceeds_ReturnsList()
        {
            var list = new List<OglasVoznjaVM> { new() { IdOglasVoznja = 1 } };
            var json = JsonConvert.SerializeObject(list);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }),
                out var sess, out _);

            var view = Assert.IsType<ViewResult>(await ctrl.Index());
            var model = Assert.IsAssignableFrom<List<OglasVoznjaVM>>(view.Model);
            Assert.Single(model);
            Assert.Equal(1, model[0].IdOglasVoznja);
        }

        [Fact]
        public async Task IndexUser_NotLoggedIn_RedirectsToLogin()
        {
            var ctrl = CreateController((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), out var sess, out _);
            var redirect = Assert.IsType<RedirectToActionResult>(await ctrl.IndexUser());
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Auth", redirect.ControllerName);
        }

        [Fact]
        public async Task IndexUser_WithData_SetsKorisnikVoznjaIdPerItem()
        {
            var rides = new List<OglasVoznjaVM>
            {
                new() { IdOglasVoznja = 10 },
                new() { IdOglasVoznja = 20 }
            };
            var ridesJson = JsonConvert.SerializeObject(rides);
            var ctrl = CreateController(async (req, ct) =>
            {
                if (req.RequestUri.PathAndQuery.StartsWith("/api/OglasVoznja/GetAllByUser"))
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ridesJson, Encoding.UTF8, "application/json") };
                // for each KorisnikVoznja/GetByUserAndRide
                var kv = new KorisnikVoznjaVM { IdKorisnikVoznja = 99 };
                var kvJson = JsonConvert.SerializeObject(kv);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(kvJson, Encoding.UTF8, "application/json") };
            }, out var sess, out _);

            sess.SetInt32("UserId", 5);

            var view = Assert.IsType<ViewResult>(await ctrl.IndexUser());
            var model = Assert.IsAssignableFrom<List<OglasVoznjaVM>>(view.Model);
            Assert.All(model, m => Assert.Equal(99, m.KorisnikVoznjaId));
        }

        [Fact]
        public async Task JoinRide_Get_ApiFails_ReturnsNotFound()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)),
                out var sess, out _);

            var result = await ctrl.JoinRide(7);
            var nf = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Ad not found.", nf.Value);
        }

        [Fact]
        public async Task JoinRide_Get_Successful_ReturnsViewWithModel()
        {
            var og = new OglasVoznjaVM { IdOglasVoznja = 8, Username = "u", Ime = "i", Prezime = "p", Marka = "m", Model = "mo", Registracija = "r", Polaziste = "A", Odrediste = "B" };
            var json = JsonConvert.SerializeObject(og);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }),
                out var sess, out _);

            var view = Assert.IsType<ViewResult>(await ctrl.JoinRide(8));
            var model = Assert.IsType<JoinRideVM>(view.Model);
            Assert.Equal(8, model.OglasVoznjaId);
            Assert.Equal("A", model.Polaziste);
        }

        [Fact]
        public async Task JoinRide_Post_InvalidModel_ReturnsView()
        {
            var ctrl = CreateController((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), out var sess, out _);
            ctrl.ModelState.AddModelError("X", "err");
            var vm = new JoinRideVM();
            var view = Assert.IsType<ViewResult>(await ctrl.JoinRide(vm));
            Assert.Same(vm, view.Model);
        }

        [Fact]
        public async Task JoinRide_Post_NotLoggedIn_AddsModelError()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)), out var sess, out _);
            var vm = new JoinRideVM();
            var view = Assert.IsType<ViewResult>(await ctrl.JoinRide(vm));
            Assert.True(ctrl.ModelState[string.Empty].Errors.Any(e => e.ErrorMessage.Contains("User must be logged in")));
        }

        [Fact]
        public async Task JoinRide_Post_ApiSucceeds_RedirectsToIndex()
        {
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, out var sess, out _);

            sess.SetInt32("UserId", 3);
            var vm = new JoinRideVM { Polaziste = "X" };
            var redirect = Assert.IsType<RedirectToActionResult>(await ctrl.JoinRide(vm));
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task JoinRide_Post_ApiFails_ReturnsViewWithError()
        {
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post)
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, out var sess, out _);

            sess.SetInt32("UserId", 4);
            var vm = new JoinRideVM { Polaziste = "X" };
            var view = Assert.IsType<ViewResult>(await ctrl.JoinRide(vm));
            Assert.True(ctrl.ModelState[string.Empty].Errors.Any());
        }
    }
}
