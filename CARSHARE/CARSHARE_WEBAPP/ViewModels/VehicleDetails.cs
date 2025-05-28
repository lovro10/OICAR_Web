// ViewModels/VehicleDetailsVM.cs
namespace CARSHARE_WEBAPP.ViewModels
{
    public class VehicleDetailsVM
    {
        // iz VoziloVM:
        public int Idvozilo { get; set; }
        public string Naziv { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string Registracija { get; set; }
        public bool? Isconfirmed { get; set; }

        // novo: lista Base64 prometnih slika
        public List<string> PrometneBase64 { get; set; } = new List<string>();
    }
}
