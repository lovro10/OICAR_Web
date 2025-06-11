using Newtonsoft.Json;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class VoziloDetailsVM
    {
        [JsonProperty("vozilo")]
        public VoziloVM Vozilo { get; set; }

        [JsonProperty("identificationImages")]
        public List<ImageVM> IdentificationImages { get; set; }
    }
}
