﻿using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace CARSHARE_WEBAPP.ViewModels
{
    public class KorisnikVM
    {
        public int IDKorisnik { get; set; }
        [Required(ErrorMessage = "First Name is required")]
        public string Ime { get; set; }
        [Required(ErrorMessage = "Last Name is required")]
        public string Prezime { get; set; }

        public DateOnly DatumRodjenja { get; set; }
        public string Password { get; set; }
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }
        public string Telefon { get; set; }
        public BitArray? IsConfirmed { get; set; }
        public UlogaVM Uloga { get; set; }
        public int? ImageVozackaID { get; set; }
        public int? ImageOsobnaID { get; set; }
        public int? ImageLiceID { get; set; }
        public ImageVM? ImageVozacka { get; set; }
        public ImageVM? ImageOsobna { get; set; }
        public ImageVM? ImageLice { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string PwdSalt { get; internal set; }
        public string PwdHash { get; internal set; }

        public string NewPassword { get; set; }

        public string ConfirmPassword { get; set; } 
    }
}
