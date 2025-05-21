namespace CARSHARE_WEBAPP.ViewModels
{
    public class PorukaVM
    {
        public int Korisnikvoznjaid { get; set; }

        public int? PutnikId { get; set; }

        public int? VozacId { get; set; }

        public string? Message { get; set; }

        public List<PorukaGetVM>? Messages { get; set; }
    }
}
