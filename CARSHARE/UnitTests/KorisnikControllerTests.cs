
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using CARSHARE_WEBAPP.Controllers;
using CARSHARE_WEBAPP.Services.Interfaces;   
using CARSHARE_WEBAPP.ViewModels;
using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.Security;

namespace CARSHARE_WEBAPP.UnitTests
{
    public class KorisnikControllerTests
    {
        private readonly Mock<IKorisnikService> _mockKorisnikService;
        private readonly KorisnikController _controller;

        public KorisnikControllerTests()
        {
            _mockKorisnikService = new Mock<IKorisnikService>(MockBehavior.Strict);


            _controller = new KorisnikController(_mockKorisnikService.Object);


            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task GetKorisnici_ShouldReturnViewWithListOfKorisnikVM()
        {
            var dummyList = new List<KorisnikVM>
            {
                new KorisnikVM
                {
                    IDKorisnik = 1,
                    Ime = "Test",
                    Prezime = "User",
                    Email = "test@example.com",
                    Username = "testuser",
                    Telefon = "0910000000",
                    DatumRodjenja = new DateOnly(1990, 1, 1),
                    PwdHash = "hash",
                    PwdSalt = "salt",
                    IsConfirmed = true,
                    Ulogaid = 2,
                    Uloga = new Uloga { Iduloga = 2, Naziv = "User" }
                }
            };

            _mockKorisnikService
                .Setup(s => s.GetKorisniciAsync())
                .ReturnsAsync(dummyList);

            var result = await _controller.GetKorisnici();

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            viewResult!.Model.Should().BeAssignableTo<List<KorisnikVM>>();

            var modelList = (List<KorisnikVM>)viewResult.Model!;
            modelList.Should().HaveCount(1);
            modelList[0].Username.Should().Be("testuser");
        }

        [Fact]
        public void Login_Get_ShouldReturnViewWithEmptyLoginVM()
        {
            var result = _controller.Login() as ViewResult;

            result.Should().NotBeNull();
            result!.Model.Should().BeOfType<LoginVM>();
            var model = (LoginVM)result.Model!;
            model.UserName.Should().BeNull();
            model.Password.Should().BeNull();
        }

        [Fact]
        public async Task Login_Post_WhenUserNotFound_ShouldAddModelErrorAndReturnView()
        {
            var loginVM = new LoginVM { UserName = "nonexistent", Password = "whatever" };

            _mockKorisnikService
                .Setup(s => s.GetKorisniciAsync())
                .ReturnsAsync(new List<KorisnikVM>());

            var result = await _controller.Login(loginVM);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            viewResult!.Model.Should().Be(loginVM);

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage
                .Should().Be("Incorrect username or password");
        }

        [Fact]
        public async Task Login_Post_WhenPasswordMismatch_ShouldAddModelErrorAndReturnView()
        {
            var saltBytes = System.Text.Encoding.UTF8.GetBytes("some‐random‐salt");
            var base64Salt = Convert.ToBase64String(saltBytes);

            var correctHash = PasswordHashProvider.GetHash("rightPass", base64Salt);
            var existing = new KorisnikVM
            {
                IDKorisnik = 7,
                Username = "existingUser",
                PwdSalt = base64Salt,
                PwdHash = correctHash,
                Uloga = new Uloga { Iduloga = 2, Naziv = "User" }
            };
            _mockKorisnikService
                .Setup(s => s.GetKorisniciAsync())
                .ReturnsAsync(new List<KorisnikVM> { existing });

            var loginVM = new LoginVM
            {
                UserName = "existingUser",
                Password = "wrongPassword"
            };

            var result = await _controller.Login(loginVM);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            viewResult!.Model.Should().BeNull(); // returns View() without a model

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage
                .Should().Be("Invalid username or password");
        }

        [Fact]
        public async Task Logout_ShouldClearCookiesAndRedirectToLogin()
        {
            var response = _controller.Response;
            response.Cookies.Append("JWToken", "token");
            response.Cookies.Append("Username", "userX");
            response.Cookies.Append("UserId", "5");
            response.Cookies.Append("Role", "User");

            var mockSession = new Mock<ISession>();
            _controller.ControllerContext.HttpContext.Session = mockSession.Object;

            var result = await _controller.Logout();

            var redirect = result as RedirectToActionResult;
            redirect.Should().NotBeNull();
            redirect!.ActionName.Should().Be("Login");
            redirect.ControllerName.Should().Be("Korisnik");

            var setCookieHeaders = response.Headers["Set-Cookie"];
            setCookieHeaders.Count.Should().BeGreaterThanOrEqualTo(4, "because Logout() deletes four cookies");

            setCookieHeaders.Should().Contain(x => x.StartsWith("JWToken=", StringComparison.OrdinalIgnoreCase));
            setCookieHeaders.Should().Contain(x => x.StartsWith("Username=", StringComparison.OrdinalIgnoreCase));
            setCookieHeaders.Should().Contain(x => x.StartsWith("UserId=", StringComparison.OrdinalIgnoreCase));
            setCookieHeaders.Should().Contain(x => x.StartsWith("Role=", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Details_WhenKorisnikExists_ShouldReturnViewWithKorisnikVM()
        {
            var dummyList = new List<KorisnikVM>
            {
                new KorisnikVM
                {
                    IDKorisnik = 3,
                    Ime = "Marko",
                    Prezime = "Markić",
                    Email = "marko@example.com",
                    Username = "markom",
                    Telefon = "0911111111",
                    DatumRodjenja = new DateOnly(1992, 2, 2),
                    PwdHash = "h",
                    PwdSalt = "s",
                    IsConfirmed = true,
                    Ulogaid = 2,
                    Uloga = new Uloga { Iduloga = 2, Naziv = "User" }
                }
            };
            _mockKorisnikService
                .Setup(s => s.GetKorisniciAsync())
                .ReturnsAsync(dummyList);

            var result = await _controller.Details(3);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            var model = viewResult!.Model as KorisnikVM;
            model.Should().NotBeNull();
            model!.IDKorisnik.Should().Be(3);
            model.Ime.Should().Be("Marko");
        }

        [Fact]
        public async Task Details_WhenKorisnikNotFound_ShouldReturnNotFound()
        {
            _mockKorisnikService.Setup(s => s.GetKorisniciAsync())
                                .ReturnsAsync(new List<KorisnikVM>());

            var result = await _controller.Details(99);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Update_Get_WhenKorisnikExists_ShouldReturnViewWithEditKorisnikVM()
        {
            var existing = new KorisnikVM
            {
                IDKorisnik = 7,
                Ime = "Ana",
                Prezime = "Anić",
                Email = "ana@example.com",
                Telefon = "0912222222",
                DatumRodjenja = new DateOnly(1991, 3, 3),
                Username = "anacic",
                PwdHash = "h",
                PwdSalt = "s",
                IsConfirmed = true,
                Ulogaid = 2,
                Uloga = new Uloga { Iduloga = 2, Naziv = "User" }
            };
            _mockKorisnikService.Setup(s => s.GetKorisniciAsync())
                                .ReturnsAsync(new List<KorisnikVM> { existing });

            var result = await _controller.Update(7);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            var model = viewResult!.Model as EditKorisnikVM;
            model.Should().NotBeNull();
            model!.IDKorisnik.Should().Be(7);
            model.Ime.Should().Be("Ana");
            model.Username.Should().Be("anacic");
        }

        [Fact]
        public async Task Update_Get_WhenKorisnikNotFound_ShouldReturnNotFound()
        {
            _mockKorisnikService.Setup(s => s.GetKorisniciAsync())
                                .ReturnsAsync(new List<KorisnikVM>());

            var result = await _controller.Update(50);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Update_Post_WhenModelStateInvalid_ShouldReturnViewWithModel()
        {
            _controller.ModelState.AddModelError("Email", "Email is required");
            var editVm = new EditKorisnikVM
            {
                IDKorisnik = 10,
                Ime = "Zoran",
                Prezime = "Zorić",
                Email = "", // invalid
                Telefon = "0913333333",
                DatumRodjenja = new DateOnly(1988, 4, 4),
                Username = "zoranz"
            };

            var result = await _controller.Update(editVm);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            var model = viewResult!.Model as EditKorisnikVM;
            model.Should().NotBeNull();
            model!.Email.Should().Be("");
        }

        [Fact]
        public async Task Update_Post_WhenServiceReturnsSuccess_ShouldRedirectToDetails()
        {
            var validModel = new EditKorisnikVM
            {
                IDKorisnik = 11,
                Ime = "Mate",
                Prezime = "Matić",
                Email = "mate@example.com",
                Telefon = "0914444444",
                DatumRodjenja = new DateOnly(1994, 5, 5),
                Username = "matem"
            };
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.OK);
            _mockKorisnikService.Setup(s => s.UpdateKorisnikAsync(It.IsAny<EditKorisnikVM>()))
                                .ReturnsAsync(fakeResponse);

            var result = await _controller.Update(validModel);

            var redirect = result as RedirectToActionResult;
            redirect.Should().NotBeNull();
            redirect!.ActionName.Should().Be("Details");
            redirect.RouteValues["id"].Should().Be(validModel.IDKorisnik);
        }

        [Fact]
        public async Task Update_Post_WhenServiceReturnsFailure_ShouldAddModelErrorAndReturnView()
        {
            var validModel = new EditKorisnikVM
            {
                IDKorisnik = 12,
                Ime = "Klara",
                Prezime = "Klarić",
                Email = "klara@example.com",
                Telefon = "0915555555",
                DatumRodjenja = new DateOnly(1993, 6, 6),
                Username = "klarak"
            };
            var fakeResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            _mockKorisnikService.Setup(s => s.UpdateKorisnikAsync(It.IsAny<EditKorisnikVM>()))
                                .ReturnsAsync(fakeResponse);

            var result = await _controller.Update(validModel);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            viewResult!.Model.Should().Be(validModel);

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage
                .Should().Be("Failed to update user. Please try again.");
        }

        [Fact]
        public async Task Update_Post_WhenServiceThrowsException_ShouldAddModelErrorAndReturnView()
        {
            var validModel = new EditKorisnikVM
            {
                IDKorisnik = 13,
                Ime = "Luka",
                Prezime = "Lukić",
                Email = "luka@example.com",
                Telefon = "0916666666",
                DatumRodjenja = new DateOnly(1989, 8, 8),
                Username = "lukal"
            };
            _mockKorisnikService.Setup(s => s.UpdateKorisnikAsync(It.IsAny<EditKorisnikVM>()))
                                .ThrowsAsync(new Exception("Service is down"));

            var result = await _controller.Update(validModel);

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            viewResult!.Model.Should().Be(validModel);

            _controller.ModelState.ErrorCount.Should().Be(1);
            _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage
                .Should().Contain("An error occurred: Service is down");
        }

        [Fact]
        public async Task Images_ShouldCallServiceWithJwtFromCookieAndReturnView()
        {
            _controller.ControllerContext.HttpContext.Request.Headers["Cookie"] = "JWToken=dummy.jwt.token";

            var dummyImages = new List<ImageVM>
            {
                new ImageVM
                {
                    IDImage = 101,
                    Name = "img1.jpg",
                    Base64Content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("d")),
                    Content = System.Text.Encoding.UTF8.GetBytes("d")
                }
            };
            _mockKorisnikService.Setup(s => s.GetImagesAsync("dummy.jwt.token"))
                                .ReturnsAsync(dummyImages);

            var result = await _controller.Images();

            var viewResult = result as ViewResult;
            viewResult.Should().NotBeNull();
            var model = viewResult!.Model as List<ImageVM>;
            model.Should().NotBeNull();
            model!.Should().HaveCount(1);
            model[0].IDImage.Should().Be(101);
        }
    }
}
