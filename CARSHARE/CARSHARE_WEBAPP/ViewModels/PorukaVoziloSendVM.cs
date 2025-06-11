namespace CARSHARE_WEBAPP.ViewModels
{
    public class PorukaVoziloSendVM
    {
        public int Korisnikvoziloid { get; set; }

        public int? PutnikId { get; set; }

        public int? VozacId { get; set; }

        public string? Message { get; set; }

        public List<PorukaVoziloGetVM> Messages { get; set; }
    }
}
