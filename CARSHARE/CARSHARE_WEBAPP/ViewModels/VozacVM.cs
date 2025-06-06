using CARSHARE_WEBAPP.ViewModels;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class VozacVM
    {
        public int Idkorisnik { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Username { get; set; }
        public List<ImageVM> Images { get; set; }
    }
}
