using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace CARSHARE_WEBAPP.Tests.Controllers
{

    public class VoziloControllerTests
    {
        private VoziloController CreateController(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc,
            out DefaultHttpContext ctx,
            out TestSession session)
        {
            var handler = new FakeHttpMessageHandler(handlerFunc);
            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5194/api/") };
            var controller = new VoziloController(Mock.Of<IHttpContextAccessor>());
            typeof(VoziloController)
                .GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(controller, client);
            ctx = new DefaultHttpContext();
            session = new TestSession();
            ctx.Session = session;
            controller.ControllerContext = new ControllerContext { HttpContext = ctx };
            return controller;
        }

        [Fact]
        public async Task Index_NoTokenOrUser_RedirectsToLogin()
        {
            var ctrl = CreateController((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out _);
            var r1 = Assert.IsType<RedirectToActionResult>(await ctrl.Index());
            Assert.Equal("Login", r1.ActionName);
            Assert.Equal("Korisnik", r1.ControllerName);

            ctx.Session.SetString("JWToken", "t");
            var r2 = Assert.IsType<RedirectToActionResult>(await ctrl.Index());
            Assert.Equal("Login", r2.ActionName);
            Assert.Equal("Korisnik", r2.ControllerName);
        }

        [Fact]
        public async Task Index_ApiFails_SetsViewBagErrorAndEmptyList()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)),
                out var ctx, out var sess);
            sess.SetString("JWToken", "t");
            sess.SetInt32("UserId", 1);

            var view = Assert.IsType<ViewResult>(await ctrl.Index());
            Assert.Equal("Unable to load vehicles.", ctrl.ViewBag.Error);
            var model = Assert.IsAssignableFrom<List<VoziloVM>>(view.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_ApiSucceeds_ReturnsList()
        {
            var list = new List<VoziloVM> { new() { Idvozilo = 5 } };
            var json = JsonConvert.SerializeObject(list);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }), out var ctx, out var sess);
            sess.SetString("JWToken", "t");
            sess.SetInt32("UserId", 2);

            var view = Assert.IsType<ViewResult>(await ctrl.Index());
            var model = Assert.IsAssignableFrom<List<VoziloVM>>(view.Model);
            Assert.Single(model);
            Assert.Equal(5, model[0].Idvozilo);
        }

        [Fact]
        public async Task Create_Get_PopulatesViewBagCars()
        {
            var brands = new List<string> { "A", "B" };
            var json = JsonConvert.SerializeObject(brands);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }), out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.Create());
            Assert.Equal(brands, ctrl.ViewBag.Cars as List<string>);
        }

        [Fact]
        public async Task Create_Post_NoImages_ShowsError()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);
            sess.SetString("JWToken", "t");
            sess.SetInt32("UserId", 3);

            var vm = new VoziloVM();
            var view = Assert.IsType<ViewResult>(await ctrl.Create(vm));
            Assert.Equal("Both images are required.", ctrl.ViewBag.Error);
            Assert.Same(vm, view.Model);
        }

        [Fact]
        public async Task Create_Post_NoToken_RedirectsToLoginAccount()
        {
            var ctrl = CreateController((req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);
            var res = Assert.IsType<RedirectToActionResult>(await ctrl.Create(new VoziloVM()));
            Assert.Equal("Login", res.ActionName);
            Assert.Equal("Account", res.ControllerName);
        }

        [Fact]
        public async Task Create_Post_Success_RedirectsToIndex()
        {
            var called = false;
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post) { called = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, out var ctx, out var sess);
            sess.SetString("JWToken", "t");
            sess.SetInt32("UserId", 4);

            var ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("x"));
            var vm = new VoziloVM { FrontImage = new FormFile(ms, 0, ms.Length, null, "f"), BackImage = new FormFile(ms, 0, ms.Length, null, "b") };

            var redirect = Assert.IsType<RedirectToActionResult>(await ctrl.Create(vm));
            Assert.True(called);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Create_Post_Fail_SetsError()
        {
            var ctrl = CreateController((req, ct) =>
            {
                if (req.Method == HttpMethod.Post) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }, out var ctx, out var sess);
            sess.SetString("JWToken", "t");
            sess.SetInt32("UserId", 5);

            var ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes("x"));
            var vm = new VoziloVM { FrontImage = new FormFile(ms, 0, ms.Length, null, "f"), BackImage = new FormFile(ms, 0, ms.Length, null, "b") };

            var view = Assert.IsType<ViewResult>(await ctrl.Create(vm));
            Assert.Equal("Failed to create vehicle.", ctrl.ViewBag.Error);
            Assert.Same(vm, view.Model);
        }

        [Fact]
        public async Task DetailsAdmin_ApiFails_ModelStateErrorAndEmpty()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
                out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.DetailsAdmin());
            Assert.False(ctrl.ModelState.IsValid);
            Assert.Empty((view.Model as List<VoziloVM>)!);
        }

        [Fact]
        public async Task DetailsAdmin_ApiSucceeds_ReturnsSorted()
        {
            var list = new List<VoziloVM> { new() { Idvozilo = 1 }, new() { Idvozilo = 3 }, new() { Idvozilo = 2 } };
            var json = JsonConvert.SerializeObject(list);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }), out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.DetailsAdmin());
            var model = Assert.IsAssignableFrom<List<VoziloVM>>(view.Model);
            Assert.Equal(new[] { 3, 2, 1 }, model.Select(v => v.Idvozilo));
        }

        [Fact]
        public async Task Details_GetFail_ShowsError()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)),
                out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.Details(9));
            Assert.Equal("Failed to load vehicle details.", ctrl.ViewBag.Error);
        }

        [Fact]
        public async Task Details_GetSuccessWithNull_ShowsError()
        {
            var vm = new VoziloDetailsVM { Vozilo = null };
            var json = JsonConvert.SerializeObject(vm);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }), out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.Details(10));
            Assert.Equal("Vehicle details not found.", ctrl.ViewBag.Error);
        }

        [Fact]
        public async Task Details_GetSuccess_ReturnsViewWithModel()
        {
            var vm = new VoziloDetailsVM { Vozilo = new VoziloVM { Idvozilo = 7 } };
            var json = JsonConvert.SerializeObject(vm);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }), out var ctx, out var sess);

            var view = Assert.IsType<ViewResult>(await ctrl.Details(11));
            var model = Assert.IsType<VoziloDetailsVM>(view.Model);
            Assert.Equal(7, model.Vozilo.Idvozilo);
        }

        [Fact]
        public async Task Delete_Get_NoToken_RedirectsToLogin()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);

            var r = Assert.IsType<RedirectToActionResult>(await ctrl.Delete(5));
            Assert.Equal("Login", r.ActionName);
            Assert.Equal("Korisnik", r.ControllerName);
        }

        [Fact]
        public async Task Delete_Get_Success_ReturnsModel()
        {
            var vm = new VoziloVM { Idvozilo = 8 };
            var json = JsonConvert.SerializeObject(vm);
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                }), out var ctx, out var sess);
            sess.SetString("JWToken", "t");

            var view = Assert.IsType<ViewResult>(await ctrl.Delete(8));
            var model = Assert.IsType<VoziloVM>(view.Model);
            Assert.Equal(8, model.Idvozilo);
        }

        [Fact]
        public async Task DeleteConfirmed_NoToken_RedirectsToLogin()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);

            var r = Assert.IsType<RedirectToActionResult>(await ctrl.DeleteConfirmed(6));
            Assert.Equal("Login", r.ActionName);
            Assert.Equal("Korisnik", r.ControllerName);
        }

        [Fact]
        public async Task DeleteConfirmed_Fail_ShowsErrorAndRedirectsIndex()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)),
                out var ctx, out var sess);
            sess.SetString("JWToken", "t");

            var r = Assert.IsType<RedirectToActionResult>(await ctrl.DeleteConfirmed(6));
            Assert.Equal("Index", r.ActionName);
            Assert.Equal("Failed to delete vehicle.", ctrl.ViewBag.Error);
        }

        [Fact]
        public async Task DeleteConfirmed_Success_RedirectsIndex()
        {
            var ctrl = CreateController((req, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
                out var ctx, out var sess);
            sess.SetString("JWToken", "t");

            var r = Assert.IsType<RedirectToActionResult>(await ctrl.DeleteConfirmed(9));
            Assert.Equal("Index", r.ActionName);
        }
    }
}
