using Newtonsoft.Json;
using System.Collections;

namespace CARSHARE_WEBAPP.Models
{
    public class Korisnik
    {
        public int IDKorisnik { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public DateOnly DatumRodjenja { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PwdHash { get; set; }
        public string PwdSalt { get; set; }
        public string Telefon { get; set; }
        public bool? IsConfirmed { get; set; }
        public int UlogaID { get; set; }
        public int? ImageVozackaID { get; set; }
        public int? ImageOsobnaID { get; set; }
        public int? ImageLiceID { get; set; }
        public DateTime? DeletedAt { get; set; }
        public virtual Uloga? Uloga { get; internal set; }
        public Image? ImageVozacka { get; set; }
        public Image? ImageOsobna{ get; set; }
        public Image? ImageLice { get; set; }
    }
}
