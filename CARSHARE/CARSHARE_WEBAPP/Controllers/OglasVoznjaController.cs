using CARSHARE_WEBAPP.Models;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

public class OglasVoznjaController : Controller
{ 
    private readonly HttpClient _httpClient;

    public OglasVoznjaController(IHttpClientFactory clientFactory)
    {
        _httpClient = clientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri("http://localhost:5194/api/"); 
    } 

    public async Task<IActionResult> Index() 
    { 
        var response = await _httpClient.GetAsync("OglasVoznja/GetAll");
        if (!response.IsSuccessStatusCode) 
            return View(new List<OglasVoznjaVM>());

        var json = await response.Content.ReadAsStringAsync();
        var list = JsonConvert.DeserializeObject<List<OglasVoznjaVM>>(json);
        return View(list);
    }

    public async Task<IActionResult> Create(int id)
    {
        Console.WriteLine($"Received Vozilo ID: {id}");

        var response = await _httpClient.GetAsync($"Vozilo/GetVehicleById/{id}");
        Console.WriteLine($"Response status code from GetById: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Vehicle not found.");
            return NotFound("Vehicle not found.");
        }

        var json = await response.Content.ReadAsStringAsync();
        var vozilo = JsonConvert.DeserializeObject<Vozilo>(json);

        if (vozilo == null || vozilo.Vozac == null)
        {
            Console.WriteLine("Vehicle or Driver data not found.");
            return NotFound("Vehicle or Driver data not found.");
        }

        Console.WriteLine($"Retrieved Vehicle: {vozilo.Marka} {vozilo.Model} with Driver: {vozilo.Vozac.Ime} {vozilo.Vozac.Prezime}");

        var model = new OglasVoznjaVM
        {
            VoziloId = vozilo.Idvozilo,
            Username = vozilo.Vozac.Username,
            Ime = vozilo.Vozac.Ime,
            Prezime = vozilo.Vozac.Prezime,
            Marka = vozilo.Marka,
            Model = vozilo.Model,
            Registracija = vozilo.Registracija
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(OglasVoznjaVM oglasVoznjaVM)
    { 
        Console.WriteLine("Received data from form:");
        Console.WriteLine($"{oglasVoznjaVM.VoziloId}");
        Console.WriteLine($"{oglasVoznjaVM.Polaziste}");
        Console.WriteLine($"{oglasVoznjaVM.Odrediste}");
        Console.WriteLine($"{oglasVoznjaVM.DatumIVrijemePolaska}"); 
        Console.WriteLine($"{oglasVoznjaVM.DatumIVrijemeDolaska}");  
        Console.WriteLine($"{oglasVoznjaVM.BrojPutnika}");   
        Console.WriteLine($"{oglasVoznjaVM.Gorivo}");
        Console.WriteLine($"{oglasVoznjaVM.Cestarina}"); 
        
        if (!ModelState.IsValid)
        {
            Console.WriteLine("Model state is invalid.");
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"ModelState error: {error.ErrorMessage}");
            }
            return View(oglasVoznjaVM);
        }

        var json = JsonConvert.SerializeObject(oglasVoznjaVM);

        Console.WriteLine("Sending POST request to API:");
        Console.WriteLine($"Data: {json}");

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("OglasVoznja/KreirajOglasVoznje", content); 

        Console.WriteLine($"Response status code from API: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response from API: {responseContent}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Ride advertisement successfully created.");
            return RedirectToAction("Index"); 
        }

        Console.WriteLine("Error while creating the ride advertisement.");
        ModelState.AddModelError("", "Error while creating the ride advertisement.");
        return View(oglasVoznjaVM); 
    }

}
