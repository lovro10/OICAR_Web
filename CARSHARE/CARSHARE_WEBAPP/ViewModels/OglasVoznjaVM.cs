using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class OglasVoznjaVM
    {
        public int IdOglasVoznja { get; set; }

        public int VoziloId { get; set; }

        public string? Polaziste { get; set; }

        public string? Odrediste { get; set; }

        public DateTime DatumIVrijemePolaska { get; set; }

        public DateTime DatumIVrijemeDolaska { get; set; }

        public decimal? Cestarina { get; set; }

        public decimal? Gorivo { get; set; }

        public int BrojPutnika { get; set; }

        public int? KorisnikId { get; set; }

        [JsonIgnore]
        public decimal? CijenaPoPutniku { get; set; }

        [JsonIgnore]
        public int TroskoviId { get; set; }

        [JsonIgnore]
        public int LokacijaId { get; set; }

        [JsonIgnore]
        public int StatusVoznjeId { get; set; }

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
        public bool IsUserInRide { get; set; } 

        [JsonIgnore] 
        public int KorisnikVoznjaId { get; set; } 
    }
}
 