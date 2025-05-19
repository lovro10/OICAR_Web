using System;
using System.Collections.Generic;

namespace CARSHARE_WEBAPP.Models;

public partial class Image
{
    public int Idimage { get; set; }

    public string Name { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    public int? Imagetypeid { get; set; }

    public virtual Imagetype? Imagetype { get; set; }

    public virtual ICollection<Korisnik> KorisnikImagelices { get; set; } = new List<Korisnik>();

    public virtual ICollection<Korisnik> KorisnikImageosobnas { get; set; } = new List<Korisnik>();

    public virtual ICollection<Korisnik> KorisnikImagevozackas { get; set; } = new List<Korisnik>();

    public virtual ICollection<Korisnikimage> Korisnikimages { get; set; } = new List<Korisnikimage>();

    public virtual ICollection<Vozilo> Vozilos { get; set; } = new List<Vozilo>();
}
