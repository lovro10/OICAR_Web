using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Xunit;

using CARSHARE_WEBAPP.Controllers; 
using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
/*
 * Trebamo testirati create(int it) pa smo napravili faktehttpmessagehandler koji se pravi da je
 * GetVehicleById/{id} endpoint
 *u svakom testu kreiramo laznog handlera
 *ovo je zapravo unit test a ne integration test jer ne pokrecemo MVC pipeline 
 *pravi integracijski test bi pokrenuo aplikaciju u memoriji
 */
namespace IntegrationTests
{

    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = _responder(request);
            return Task.FromResult(response);
        }
    }


    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

    public class OglasVoznjaControllerTests
    {

        [Fact]
        public async Task Create_VehicleNotFound_ReturnsNotFoundObjectResult()
        {
            var handler = new FakeHttpMessageHandler(req =>
            {
                if (req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.EndsWith("/Vozilo/GetVehicleById/999", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent(
                            "\"Vehicle not found.\"",
                            Encoding.UTF8,
                            "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(
                        "\"Vehicle not found.\"",
                        Encoding.UTF8,
                        "application/json")
                };
            });

            var fakeHttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var fakeFactory = new FakeHttpClientFactory(fakeHttpClient);

            var controller = new OglasVoznjaController(fakeFactory);

            var result = await controller.Create(999);

            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be("Vehicle not found.");
        }


        [Fact]
        public async Task Create_VehicleExists_ReturnsViewResultWithExpectedViewModel()
        {
            var vozac = new Korisnik
            {
                Username = "zavaa",
                Ime = "Luka",
                Prezime = "Zavrski"
            };

            var vozilo = new Vozilo
            {
                Idvozilo = 1,
                Marka = "Toyota",
                Model = "Corolla",
                Registracija = "ZG1234AB",
                Vozac = vozac
            };

            var jsonPayload = JsonConvert.SerializeObject(vozilo);

            var handler = new FakeHttpMessageHandler(req =>
            {
                if (req.Method == HttpMethod.Get &&
                    req.RequestUri!.AbsolutePath.EndsWith("/Vozilo/GetVehicleById/1", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("\"Vehicle not found.\"", Encoding.UTF8, "application/json")
                };
            });

            var fakeHttpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var fakeFactory = new FakeHttpClientFactory(fakeHttpClient);

            var controller = new OglasVoznjaController(fakeFactory);

            var result = await controller.Create(1);

            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewName.Should().BeNull("calling `return View(model);` leaves ViewName null");

            viewResult.Model.Should().BeOfType<OglasVoznjaVM>();
            var vm = (OglasVoznjaVM)viewResult.Model!;

            vm.VoziloId.Should().Be(1);
            vm.Username.Should().Be("zavaa");
            vm.Ime.Should().Be("Luka");
            vm.Prezime.Should().Be("Zavrski");
            vm.Marka.Should().Be("Toyota");
            vm.Model.Should().Be("Corolla");
            vm.Registracija.Should().Be("ZG1234AB");
        }
    }
}
