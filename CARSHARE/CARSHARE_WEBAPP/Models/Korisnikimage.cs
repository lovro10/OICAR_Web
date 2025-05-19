using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Korisnikimage
{
    public int Idkorisnikimage { get; set; }

    public int? Korisnikid { get; set; }

    public int? Imageid { get; set; }

    public virtual Image? Image { get; set; }

    public virtual Korisnik? Korisnik { get; set; }
}
