namespace CARSHARE_WEBAPP.Models
{
    public static class MockDB
    {
        private static List<Korisnik> _korisnici = new List<Korisnik>
    {
       new Korisnik { IDKorisnik = 1, Ime = "Test", Prezime = "User", Username = "testuser", PwdHash = "hashedpassword", PwdSalt = "salt", UlogaID = 1, Email = "Test@mail.com" },
       new Korisnik { IDKorisnik = 2, Ime = "Admin", Prezime = "User", Username = "admin", PwdHash = "adminhash", PwdSalt = "salt", UlogaID = 2, Email = "Admin@mail.com" }
    };

        private static List<Uloga> _uloge = new List<Uloga>
      {
            new Uloga {IDUloga = 1, Naziv = "PASSENGER"},
            new Uloga {IDUloga = 2, Naziv = "ADMIN"}
      };

        private static List<Vozilo> _vozila = new List<Vozilo>
        {
            new Vozilo { Idvozilo = 1, Marka= "BMW", Model = "320D", Registracija = "ZG6188JP" },
            new Vozilo { Idvozilo = 2, Marka= "AUDI", Model = "A4",  Registracija = "ZG1602KA" },
            new Vozilo { Idvozilo = 3, Marka= "MERCEDES", Model = "C", Registracija = "ZG1177KG" },
            new Vozilo { Idvozilo = 4, Marka= "VOLKSWAGEN", Model = "PASSAT", Registracija = "ZG8633HM" }
        };

        private static List<Oglasvoznja> _voznje = new List<Oglasvoznja>
        {
            new Oglasvoznja { Idoglasvoznja = 1,
                Voziloid = 1,
                DatumIVrijemePolaska = DateTime.Now,
                DatumIVrijemeDolaska = DateTime.Now,
                Troskoviid = 1,
                BrojPutnika = 2,
                Statusvoznjeid = 1,
                Lokacijaid = 1 },
            new Oglasvoznja {  Idoglasvoznja = 2,
                Voziloid = 2,
                DatumIVrijemePolaska = DateTime.Now,
                DatumIVrijemeDolaska = DateTime.Now,
                Troskoviid = 2,
                BrojPutnika = 3,
                Statusvoznjeid = 2,
                Lokacijaid = 2 },
            new Oglasvoznja { Idoglasvoznja = 3,
                Voziloid = 3,
                DatumIVrijemePolaska = DateTime.Now,
                DatumIVrijemeDolaska = DateTime.Now,
                Troskoviid = 3,
                BrojPutnika = 4,
                Statusvoznjeid = 3,
                Lokacijaid = 3
            },
                new Oglasvoznja { Idoglasvoznja = 4,
                Voziloid = 1,
                DatumIVrijemePolaska = DateTime.Now,
                DatumIVrijemeDolaska = DateTime.Now,
                Troskoviid = 1,
                BrojPutnika = 2,
                Statusvoznjeid = 1,
                Lokacijaid = 1
                }
        };
        public static List<Korisnik> GetKorisnici() => _korisnici;
        public static List<Uloga> GetUlogas() => _uloge;
        public static List<Vozilo> GetVozila() => _vozila;
        public static List<Oglasvoznja> GetVoznje() => _voznje;
    }
}
