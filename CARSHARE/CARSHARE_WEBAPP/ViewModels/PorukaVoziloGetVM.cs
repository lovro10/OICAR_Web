namespace CARSHARE_WEBAPP.ViewModels
{
    public class PorukaVoziloGetVM
    {
        public int Idporuka { get; set; }

        public string? Content { get; set; }

        public int KorisnikVoziloId { get; set; }

        public int? PutnikId { get; set; }

        public int? VozacId { get; set; }

        public string? SenderName { get; set; }

        public string? SenderRole { get; set; }
    }
}
