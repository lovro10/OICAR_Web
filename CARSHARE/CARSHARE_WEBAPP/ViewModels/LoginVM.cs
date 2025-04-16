using System.ComponentModel.DataAnnotations;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "UserName je potreban")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password je potreban")]
        [StringLength(256, MinimumLength = 8, ErrorMessage = "Password bi trebao biti 8 karaktera dug")]
        public string Password { get; set; }

   
    }
}
