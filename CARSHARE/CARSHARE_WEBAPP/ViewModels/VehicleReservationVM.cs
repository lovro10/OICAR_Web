using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class VehicleReservationVM
    {
        public int Idkorisnikvozilo { get; set; }

        public int OglasVoziloId { get; set; }

        public int KorisnikId { get; set; }

        public DateTime DatumPocetkaRezervacije { get; set; }

        public DateTime DatumZavrsetkaRezervacije { get; set; }

        [JsonIgnore]
        public DateTime DozvoljeniPocetak { get; set; }

        [JsonIgnore]
        public DateTime DozvoljeniKraj { get; set; }

        [JsonIgnore]
        public List<DateTime>? ReservedDates { get; set; } 
    }
} 
