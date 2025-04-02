namespace CARSHARE_WEBAPP.ViewModels
{
    public class KorisnikVM
    {
        public int IDKorisnik { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Telefon { get; set; }
        public bool? IsConfirmed { get; set; }
        public UlogaVM Uloga { get; set; }
        public ImageVM ImageVozacka { get; set; }
        public ImageVM ImageOsobna { get; set; }
        public ImageVM ImageLice { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
