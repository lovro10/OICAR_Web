using System.Text.Json.Serialization;
using CARSHARE_WEBAPP.Models;
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

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Ime { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? Prezime { get; set; }
        
        public IFormFile? FrontImage { get; set; }

        public IFormFile? BackImage { get; set; }
        
        public string? Username { get; set; }
        
        public string? ImagePrometnaBase64 { get; set; }
        
        public ImageVM? Imageprometna { get; set; }
        
        public string? Prometna { get; set; }
        
        public IFormFile? PrometnaFile { get; set; }
        
        public int IDVozilo { get; set; }

        public VozacVM Vozac { get; set; }
    }
}
