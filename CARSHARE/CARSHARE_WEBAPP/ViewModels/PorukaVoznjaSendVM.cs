namespace CARSHARE_WEBAPP.ViewModels
{
    public class PorukaVoznjaSendVM
    {
        public int Korisnikvoznjaid { get; set; }

        public int? PutnikId { get; set; }

        public int? VozacId { get; set; }

        public string? Message { get; set; }

        public List<PorukaVoznjaGetVM> Messages { get; set; }
    }
}
