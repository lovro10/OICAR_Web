using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Korisnik
{
    public int Idkorisnik { get; set; }

    public string Ime { get; set; } = null!;

    public string Prezime { get; set; } = null!;

    public DateOnly Datumrodjenja { get; set; }

    public string Email { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Pwdhash { get; set; } = null!;

    public string Pwdsalt { get; set; } = null!;

    public string? Telefon { get; set; }

    public int? Ulogaid { get; set; }

    public int? Imagevozackaid { get; set; }

    public int? Imageosobnaid { get; set; }

    public int? Imageliceid { get; set; }

    public DateTime? Deletedat { get; set; }

    public bool? Isconfirmed { get; set; }

    public virtual Image? Imagelice { get; set; }

    public virtual Image? Imageosobna { get; set; }

    public virtual Image? Imagevozacka { get; set; }

    public virtual ICollection<Korisnikimage> Korisnikimages { get; set; } = new List<Korisnikimage>();

    public virtual ICollection<Korisnikvozilo> Korisnikvozilos { get; set; } = new List<Korisnikvozilo>();

    public virtual ICollection<Korisnikvoznja> Korisnikvoznjas { get; set; } = new List<Korisnikvoznja>();

    public virtual ICollection<Poruka> PorukaPutniks { get; set; } = new List<Poruka>();

    public virtual ICollection<Poruka> PorukaVozacs { get; set; } = new List<Poruka>();

    public virtual Uloga? Uloga { get; set; }

    public virtual ICollection<Vozilo> Vozilos { get; set; } = new List<Vozilo>();
}
