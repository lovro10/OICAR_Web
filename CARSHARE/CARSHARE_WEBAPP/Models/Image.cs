using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.Models
{
    public class Image
    {
        
        public int IDImage { get; set; }
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }
}
