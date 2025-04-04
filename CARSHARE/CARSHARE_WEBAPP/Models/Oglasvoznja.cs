using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.Models
{
    public class Oglasvoznja
    {
        public int Idoglasvoznja { get; set; }

        public int? Voziloid { get; set; }

        public DateTime DatumIVrijemePolaska { get; set; }

        public DateTime DatumIVrijemeDolaska { get; set; }

        public int? Troskoviid { get; set; }

        public int BrojPutnika { get; set; }

        public int? Statusvoznjeid { get; set; }

        public int? Lokacijaid { get; set; } 
    }
}
