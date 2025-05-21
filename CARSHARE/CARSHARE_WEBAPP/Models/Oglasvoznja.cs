using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Oglasvoznja
{
    public int Idoglasvoznja { get; set; }

    public int? Voziloid { get; set; }

    public DateTime DatumIVrijemePolaska { get; set; }

    public DateTime DatumIVrijemeDolaska { get; set; }

    public int? Troskoviid { get; set; }

    public int BrojPutnika { get; set; }

    public int? Statusvoznjeid { get; set; }

    public int? Lokacijaid { get; set; }

    public virtual ICollection<Korisnikvoznja> Korisnikvoznjas { get; set; } = new List<Korisnikvoznja>();

    public virtual Lokacija? Lokacija { get; set; }

    public virtual Statusvoznje? Statusvoznje { get; set; }

    public virtual Troskovi? Troskovi { get; set; }

    public virtual Vozilo? Vozilo { get; set; }
}
