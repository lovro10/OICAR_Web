using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Troskovi
{
    public int Idtroskovi { get; set; }

    public decimal? Cestarina { get; set; }

    public decimal? Gorivo { get; set; }

    public virtual ICollection<Oglasvoznja> Oglasvoznjas { get; set; } = new List<Oglasvoznja>();
}
