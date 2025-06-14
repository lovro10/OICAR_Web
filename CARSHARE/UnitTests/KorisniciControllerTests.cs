using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RichardSzalay.MockHttp;
using Xunit;

namespace CARSHARE_WEBAPP.Tests.Controllers
{
    public class KorisnikControllerTests
    {
        private KorisnikController BuildController(
            IEnumerable<KorisnikVM> seedUsers,
            HttpStatusCode updateStatus = HttpStatusCode.OK)
        {
            var usersJson = JsonSerializer.Serialize(seedUsers ?? Array.Empty<KorisnikVM>());

            var handler = new MockHttpMessageHandler();
            handler.When(HttpMethod.Get, "*").Respond("application/json", usersJson);
            handler.When(HttpMethod.Put, "*").Respond(updateStatus);

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5194/api/Korisnik/") };

            // MOCK IHttpClientFactory
            var httpClientFactory = new Moq.Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("KorisnikClient")).Returns(httpClient);

            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var service = new KorisnikService(httpClient, accessor);

            var ctrl = new KorisnikController(service, httpClientFactory.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = accessor.HttpContext }
            };

            return ctrl;
        }

        [Fact]
        public async Task GetKorisnici_vraća_view_s_popisom()
        {
            var list = new List<KorisnikVM>
            {
                new() { IDKorisnik = 1, Username = "ana" },
                new() { IDKorisnik = 2, Username = "ivan" }
            };

            var sut = BuildController(list);
            var res = await sut.GetKorisnici();
            var view = Assert.IsType<ViewResult>(res);
            var mdl = Assert.IsAssignableFrom<IEnumerable<KorisnikVM>>(view.Model);
            Assert.Equal(2, new List<KorisnikVM>(mdl).Count);
        }

        [Fact]
        public async Task Update_GET_postojeći_korisnik_vraća_view()
        {
            var users = new List<KorisnikVM>
            {
                new() { IDKorisnik = 3, Ime = "Marko", Prezime = "M" }
            };

            var sut = BuildController(users);
            var res = await sut.Update(3);
            var view = Assert.IsType<ViewResult>(res);
            var mdl = Assert.IsType<EditKorisnikVM>(view.Model);
            Assert.Equal(3, mdl.IDKorisnik);
        }

        [Fact]
        public async Task Update_GET_nepoznati_korisnik_vraća_404()
        {
            var sut = BuildController(Array.Empty<KorisnikVM>());
            var res = await sut.Update(99);
            Assert.IsType<NotFoundResult>(res);
        }

        [Fact]
        public async Task Update_POST_neispravan_model_vraća_isti_view()
        {
            var sut = BuildController(Array.Empty<KorisnikVM>());
            sut.ModelState.AddModelError("Ime", "Required");

            var vm = new EditKorisnikVM { IDKorisnik = 1 };
            var res = await sut.Update(vm);
            var view = Assert.IsType<ViewResult>(res);
            Assert.Equal(vm, view.Model);
        }

        [Fact]
        public async Task Update_POST_ok_redirecta_na_Profile()
        {
            var sut = BuildController(Array.Empty<KorisnikVM>());

            var vm = new EditKorisnikVM
            {
                IDKorisnik = 4,
                Ime = "I",
                Prezime = "P",
                Email = "e@e",
                Telefon = "123"
            };

            var res = await sut.Update(vm);
            var redir = Assert.IsType<RedirectToActionResult>(res);
            Assert.Equal("Profile", redir.ActionName);
            Assert.Equal(4, redir.RouteValues["id"]);
        }

        [Fact]
        public async Task Update_POST_greška_vraća_view_s_porukom()
        {
            var sut = BuildController(Array.Empty<KorisnikVM>(), HttpStatusCode.BadRequest);

            var vm = new EditKorisnikVM { IDKorisnik = 5 };
            var res = await sut.Update(vm);
            var view = Assert.IsType<ViewResult>(res);
            Assert.Equal(vm, view.Model);
            Assert.False(sut.ModelState.IsValid);
        }

        [Fact]
        public async Task ClearDataRequestPage_GET_postojeći_korisnik_vraća_view()
        {
            var users = new List<KorisnikVM>
            {
                new() { IDKorisnik = 7, Username = "user7" }
            };

            var sut = BuildController(users);
            var res = await sut.ClearDataRequestPage(7);
            var view = Assert.IsType<ViewResult>(res);
            var mdl = Assert.IsType<KorisnikVM>(view.Model);
            Assert.Equal(7, mdl.IDKorisnik);
        }

        [Fact]
        public async Task ClearDataRequestPage_GET_nepoznati_korisnik_vraća_404()
        {
            var sut = BuildController(Array.Empty<KorisnikVM>());
            var res = await sut.ClearDataRequestPage(123);
            Assert.IsType<NotFoundResult>(res);
        }
    }
}
