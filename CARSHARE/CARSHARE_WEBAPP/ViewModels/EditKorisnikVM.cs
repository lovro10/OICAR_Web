using System.ComponentModel.DataAnnotations;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class EditKorisnikVM
    {
        public int IDKorisnik { get; set; }

        public string Ime { get; set; }

        public string Prezime { get; set; }

        public string Email { get; set; }

        public string Username { get; set; }

        public string Telefon { get; set; }

        public DateOnly DatumRodjenja { get; set; }

 
    }
}
