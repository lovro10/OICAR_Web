namespace CARSHARE_WEBAPP.ViewModels 
{
    public class OglasVoziloDisplayVM
    {
        public int Idoglasvozilo { get; set; }

        public int VoziloId { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string Registracija { get; set; }

        public DateTime DatumPocetkaRezervacije { get; set; }

        public DateTime DatumZavrsetkaRezervacije { get; set; }

        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }

    }
}
