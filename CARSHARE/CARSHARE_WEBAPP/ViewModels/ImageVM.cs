namespace CARSHARE_WEBAPP.ViewModels
{
    public class ImageVM
    {
        public int IDImage { get; set; }
        public string Name { get; set; } = "";
        public string Base64Content { get; set; } = "";
        public byte[] Content { get; set; }

    }
}
