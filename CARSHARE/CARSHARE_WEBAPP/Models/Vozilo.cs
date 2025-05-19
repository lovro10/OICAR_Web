using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Vozilo
{
    public int Idvozilo { get; set; }

    public string Marka { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string Registracija { get; set; } = null!;

    public int? Imageprometnaid { get; set; }

    public int? Vozacid { get; set; }

    public bool? Isconfirmed { get; set; }

    public string? Naziv { get; set; }

    public virtual Image? Imageprometna { get; set; }

    public virtual ICollection<Oglasvozilo> Oglasvozilos { get; set; } = new List<Oglasvozilo>();

    public virtual ICollection<Oglasvoznja> Oglasvoznjas { get; set; } = new List<Oglasvoznja>();

    public virtual Korisnik? Vozac { get; set; }
}
