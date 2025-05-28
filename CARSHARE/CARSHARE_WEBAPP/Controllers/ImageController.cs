using CARSHARE_WEBAPP.Services;
using CARSHARE_WEBAPP.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CARSHARE_WEBAPP.Controllers
{
    public class ImageController : Controller
    {
        private readonly ImageService _imageService;
        public ImageController(ImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // dohvati JWT iz sessiona ako treba autentikacija
            var jwt = HttpContext.Session.GetString("JWToken");
            var images = await _imageService.GetAllImagesAsync(jwt);
            return View(images);
        }
    }
}
