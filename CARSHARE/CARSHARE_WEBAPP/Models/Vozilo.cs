namespace CARSHARE_WEBAPP.Models
{
    public class Vozilo
    {
        public int Idvozilo { get; set; }

        public string Marka { get; set; } = null!;

        public string Model { get; set; } = null!;

        public string Registracija { get; set; } = null!;

        public int Imageprometnaid { get; set; }

        public virtual Image Imageprometna { get; set; } = null!;

        public virtual ICollection<Oglasvozilo> Oglasvozilos { get; set; } = new List<Oglasvozilo>();

        public virtual ICollection<Oglasvoznja> Oglasvoznjas { get; set; } = new List<Oglasvoznja>();
    }
}
