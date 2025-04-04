namespace CARSHARE_WEBAPP.Models
{
    public class Lokacija
    {
        public int Idlokacija { get; set; }

        public string Polaziste { get; set; } = null!;

        public string Odrediste { get; set; } = null!;

        public virtual ICollection<Oglasvoznja> Oglasvoznjas { get; set; } = new List<Oglasvoznja>();
    }
}
