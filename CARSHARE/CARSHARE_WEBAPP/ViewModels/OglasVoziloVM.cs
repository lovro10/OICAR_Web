using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class OglasVoziloVM
    {
        public int Idoglasvozilo { get; set; }

        public int VoziloId { get; set; }
        
        public DateTime DatumPocetkaRezervacije { get; set; }
        
        public DateTime DatumZavrsetkaRezervacije { get; set; }

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
    }
}
