namespace CARSHARE_WEBAPP.Models
{
    public static class MockDB
    {
        private static List<Korisnik> _korisnici = new List<Korisnik>
    {
       new Korisnik { IDKorisnik = 1, Ime = "Test", Prezime = "User", Username = "testuser", PwdHash = "hashedpassword", PwdSalt = "salt", UlogaID = 1 },
       new Korisnik { IDKorisnik = 2, Ime = "Admin", Prezime = "User", Username = "admin", PwdHash = "adminhash", PwdSalt = "salt", UlogaID = 2 }
    };

        private static List<Uloga> _uloge = new List<Uloga>
      {
            new Uloga {IDUloga = 1, Naziv = "PASSENGER"},
            new Uloga {IDUloga = 2, Naziv = "ADMIN"}
      }; 

        public static List<Korisnik> GetKorisnici() => _korisnici;
        public static List<Uloga> GetUlogas() => _uloge;
    }
}
