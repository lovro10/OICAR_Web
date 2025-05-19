using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.ViewModels 
{
    public class VoziloVM 
    {
        public int Idvozilo { get; set; }

        public string? Naziv { get; set; }

        public string? Marka { get; set; }

        public string? Model { get; set; }

        public string? Registracija { get; set; }

        public int VozacId { get; set; }

        [JsonIgnore]
        public bool? Isconfirmed { get; set; }

        public IFormFile? FrontImage { get; set; }

        public IFormFile? BackImage { get; set; }
    }
}
