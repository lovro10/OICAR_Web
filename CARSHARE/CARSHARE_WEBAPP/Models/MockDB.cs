namespace CARSHARE_WEBAPP.Models
{
    public static class MockDB
    {
        private static List<Korisnik> _korisnici = new List<Korisnik>
    {
        new Korisnik { IDKorisnik = 1, Ime = "Marko", Prezime = "Markovic", DatumRodjenja = new DateTime(1990, 5, 21),
            Email = "marko@example.com", Username = "marko123", PwdHash = "hashedpassword", PwdSalt = "salt",
            Telefon = "123-456-789", IsConfirmed = true, UlogaID = 1 }
    };

        public static List<Korisnik> GetKorisnici() => _korisnici;
    }
}
