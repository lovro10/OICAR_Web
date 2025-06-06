using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.ViewModels
{ 
    public class JoinRideVM 
    {
        public int? KorisnikId { get; set; }

        public int? OglasVoznjaId { get; set; }

        public string? LokacijaPutnik { get; set; }

        public string? LokacijaVozac { get; set; }

        [JsonIgnore]
        public string? Username { get; set; }

        [JsonIgnore]
        public string? Ime { get; set; }

        [JsonIgnore]
        public string? Prezime { get; set; }

        [JsonIgnore]
        public string? Marka { get; set; }

        [JsonIgnore]
        public string? Model { get; set; }

        [JsonIgnore]
        public string? Registracija { get; set; }

        [JsonIgnore]
        public string? Polaziste { get; set; }
    
        [JsonIgnore] 
        public string? Odrediste { get; set; }
    }
}
