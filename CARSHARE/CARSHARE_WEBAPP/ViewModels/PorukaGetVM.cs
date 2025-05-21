namespace CARSHARE_WEBAPP.ViewModels
{
    public class PorukaGetVM
    {
        public int Idporuka { get; set; }

        public string? Content { get; set; }

        public int KorisnikVoznjaId { get; set; }

        public int? PutnikId { get; set; }

        public int? VozacId { get; set; }

        public string? SenderName { get; set; }
    
        public string? SenderRole { get; set; }

        public DateTime SentAt { get; set; }
    }
}
