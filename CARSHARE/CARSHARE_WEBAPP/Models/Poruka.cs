using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Poruka
{
    public int Idporuka { get; set; }

    public int? Korisnikvoznjaid { get; set; }

    public int? Putnikid { get; set; }

    public int? Vozacid { get; set; }

    public string Content { get; set; } = null!;

    public virtual Korisnikvoznja? Korisnikvoznja { get; set; }

    public virtual Korisnik? Putnik { get; set; }

    public virtual Korisnik? Vozac { get; set; }
}
