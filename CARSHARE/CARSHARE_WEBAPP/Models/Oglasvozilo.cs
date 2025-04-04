using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.Models
{
    public class Oglasvozilo
    {
        public int Idoglasvozilo { get; set; }

        public int? Voziloid { get; set; }

        public DateTime DatumPocetkaRezervacije { get; set; }

        public DateTime DatumZavrsetkaRezervacije { get; set; }

        public int? Korisnikid { get; set; }

        public virtual Korisnik? Korisnik { get; set; }

        public virtual Vozilo? Vozilo { get; set; }
    }
}
