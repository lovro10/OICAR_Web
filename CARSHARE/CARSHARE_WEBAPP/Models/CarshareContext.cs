using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CARSHARE_WEBAPP.Models;

public partial class CarshareContext : DbContext
{
    public CarshareContext()
    {
    }

    public CarshareContext(DbContextOptions<CarshareContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Imagetype> Imagetypes { get; set; }

    public virtual DbSet<Korisnik> Korisniks { get; set; }

    public virtual DbSet<Korisnikimage> Korisnikimages { get; set; }

    public virtual DbSet<Korisnikvozilo> Korisnikvozilos { get; set; }

    public virtual DbSet<Korisnikvoznja> Korisnikvoznjas { get; set; }

    public virtual DbSet<Lokacija> Lokacijas { get; set; }

    public virtual DbSet<Oglasvozilo> Oglasvozilos { get; set; }

    public virtual DbSet<Oglasvoznja> Oglasvoznjas { get; set; }

    public virtual DbSet<Poruka> Porukas { get; set; }

    public virtual DbSet<Statusvoznje> Statusvoznjes { get; set; }

    public virtual DbSet<Temp> Temps { get; set; }

    public virtual DbSet<Troskovi> Troskovis { get; set; }

    public virtual DbSet<Uloga> Ulogas { get; set; }

    public virtual DbSet<Vozilo> Vozilos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=strongly-efficient-echidna.data-1.use1.tembo.io;TrustServerCertificate=False;Port=5432;Username=postgres;Password=86WSYOqZEcLvfAWi;Database=carshare");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_stat_statements");

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Idimage).HasName("image_pkey");

            entity.ToTable("image");

            entity.Property(e => e.Idimage).HasColumnName("idimage");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Imagetypeid).HasColumnName("imagetypeid");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Imagetype).WithMany(p => p.Images)
                .HasForeignKey(d => d.Imagetypeid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("image_imagetypeid_fkey");
        });

        modelBuilder.Entity<Imagetype>(entity =>
        {
            entity.HasKey(e => e.Idimagetype).HasName("imagetype_pkey");

            entity.ToTable("imagetype");

            entity.Property(e => e.Idimagetype).HasColumnName("idimagetype");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Korisnik>(entity =>
        {
            entity.HasKey(e => e.Idkorisnik).HasName("korisnik_pkey");

            entity.ToTable("korisnik");

            entity.HasIndex(e => e.Email, "korisnik_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "korisnik_username_key").IsUnique();

            entity.Property(e => e.Idkorisnik).HasColumnName("idkorisnik");
            entity.Property(e => e.Datumrodjenja).HasColumnName("datumrodjenja");
            entity.Property(e => e.Deletedat)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("deletedat");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Imageliceid).HasColumnName("imageliceid");
            entity.Property(e => e.Imageosobnaid).HasColumnName("imageosobnaid");
            entity.Property(e => e.Imagevozackaid).HasColumnName("imagevozackaid");
            entity.Property(e => e.Ime)
                .HasMaxLength(255)
                .HasColumnName("ime");
            entity.Property(e => e.Isconfirmed).HasColumnName("isconfirmed");
            entity.Property(e => e.Prezime)
                .HasMaxLength(255)
                .HasColumnName("prezime");
            entity.Property(e => e.Pwdhash)
                .HasMaxLength(255)
                .HasColumnName("pwdhash");
            entity.Property(e => e.Pwdsalt)
                .HasMaxLength(255)
                .HasColumnName("pwdsalt");
            entity.Property(e => e.Telefon)
                .HasMaxLength(255)
                .HasColumnName("telefon");
            entity.Property(e => e.Ulogaid)
                .HasDefaultValue(1)
                .HasColumnName("ulogaid");
            entity.Property(e => e.Username)
                .HasMaxLength(255)
                .HasColumnName("username");

            entity.HasOne(d => d.Imagelice).WithMany(p => p.KorisnikImagelices)
                .HasForeignKey(d => d.Imageliceid)
                .HasConstraintName("korisnik_imageliceid_fkey");

            entity.HasOne(d => d.Imageosobna).WithMany(p => p.KorisnikImageosobnas)
                .HasForeignKey(d => d.Imageosobnaid)
                .HasConstraintName("korisnik_imageosobnaid_fkey");

            entity.HasOne(d => d.Imagevozacka).WithMany(p => p.KorisnikImagevozackas)
                .HasForeignKey(d => d.Imagevozackaid)
                .HasConstraintName("korisnik_imagevozackaid_fkey");

            entity.HasOne(d => d.Uloga).WithMany(p => p.Korisniks)
                .HasForeignKey(d => d.Ulogaid)
                .HasConstraintName("korisnik_ulogaid_fkey");
        });

        modelBuilder.Entity<Korisnikimage>(entity =>
        {
            entity.HasKey(e => e.Idkorisnikimage).HasName("korisnikimage_pkey");

            entity.ToTable("korisnikimage");

            entity.Property(e => e.Idkorisnikimage).HasColumnName("idkorisnikimage");
            entity.Property(e => e.Imageid).HasColumnName("imageid");
            entity.Property(e => e.Korisnikid).HasColumnName("korisnikid");

            entity.HasOne(d => d.Image).WithMany(p => p.Korisnikimages)
                .HasForeignKey(d => d.Imageid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("korisnikimage_imageid_fkey");

            entity.HasOne(d => d.Korisnik).WithMany(p => p.Korisnikimages)
                .HasForeignKey(d => d.Korisnikid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("korisnikimage_korisnikid_fkey");
        });

        modelBuilder.Entity<Korisnikvozilo>(entity =>
        {
            entity.HasKey(e => e.Idkorisnikvozilo).HasName("korisnikvozilo_pkey");

            entity.ToTable("korisnikvozilo");

            entity.Property(e => e.Idkorisnikvozilo).HasColumnName("idkorisnikvozilo");
            entity.Property(e => e.DatumPocetkaRezervacije)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datum_pocetka_rezervacije");
            entity.Property(e => e.DatumZavrsetkaRezervacije)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datum_zavrsetka_rezervacije");
            entity.Property(e => e.Korisnikid).HasColumnName("korisnikid");
            entity.Property(e => e.Oglasvoziloid).HasColumnName("oglasvoziloid");

            entity.HasOne(d => d.Korisnik).WithMany(p => p.Korisnikvozilos)
                .HasForeignKey(d => d.Korisnikid)
                .HasConstraintName("korisnikvozilo_korisnikid_fkey");

            entity.HasOne(d => d.Oglasvozilo).WithMany(p => p.Korisnikvozilos)
                .HasForeignKey(d => d.Oglasvoziloid)
                .HasConstraintName("korisnikvozilo_oglasvoziloid_fkey");
        });

        modelBuilder.Entity<Korisnikvoznja>(entity =>
        {
            entity.HasKey(e => e.Idkorisnikvoznja).HasName("korisnikvoznja_pkey");

            entity.ToTable("korisnikvoznja");

            entity.Property(e => e.Idkorisnikvoznja).HasColumnName("idkorisnikvoznja");
            entity.Property(e => e.Korisnikid).HasColumnName("korisnikid");
            entity.Property(e => e.Lokacijaputnik).HasColumnName("lokacijaputnik");
            entity.Property(e => e.Lokacijavozac).HasColumnName("lokacijavozac");
            entity.Property(e => e.Oglasvoznjaid).HasColumnName("oglasvoznjaid");

            entity.HasOne(d => d.Korisnik).WithMany(p => p.Korisnikvoznjas)
                .HasForeignKey(d => d.Korisnikid)
                .HasConstraintName("korisnikvoznja_korisnikid_fkey");

            entity.HasOne(d => d.Oglasvoznja).WithMany(p => p.Korisnikvoznjas)
                .HasForeignKey(d => d.Oglasvoznjaid)
                .HasConstraintName("korisnikvoznja_oglasvoznjaid_fkey");
        });

        modelBuilder.Entity<Lokacija>(entity =>
        {
            entity.HasKey(e => e.Idlokacija).HasName("lokacija_pkey");

            entity.ToTable("lokacija");

            entity.Property(e => e.Idlokacija).HasColumnName("idlokacija");
            entity.Property(e => e.Odrediste).HasColumnName("odrediste");
            entity.Property(e => e.Polaziste).HasColumnName("polaziste");
        });

        modelBuilder.Entity<Oglasvozilo>(entity =>
        {
            entity.HasKey(e => e.Idoglasvozilo).HasName("oglasvozilo_pkey");

            entity.ToTable("oglasvozilo");

            entity.Property(e => e.Idoglasvozilo).HasColumnName("idoglasvozilo");
            entity.Property(e => e.DatumPocetkaRezervacije)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datum_pocetka_rezervacije");
            entity.Property(e => e.DatumZavrsetkaRezervacije)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datum_zavrsetka_rezervacije");
            entity.Property(e => e.Voziloid).HasColumnName("voziloid");

            entity.HasOne(d => d.Vozilo).WithMany(p => p.Oglasvozilos)
                .HasForeignKey(d => d.Voziloid)
                .HasConstraintName("oglasvozilo_voziloid_fkey");
        });

        modelBuilder.Entity<Oglasvoznja>(entity =>
        {
            entity.HasKey(e => e.Idoglasvoznja).HasName("oglasvoznja_pkey");

            entity.ToTable("oglasvoznja");

            entity.Property(e => e.Idoglasvoznja).HasColumnName("idoglasvoznja");
            entity.Property(e => e.BrojPutnika).HasColumnName("broj_putnika");
            entity.Property(e => e.DatumIVrijemeDolaska)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datum_i_vrijeme_dolaska");
            entity.Property(e => e.DatumIVrijemePolaska)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("datum_i_vrijeme_polaska");
            entity.Property(e => e.Lokacijaid).HasColumnName("lokacijaid");
            entity.Property(e => e.Statusvoznjeid).HasColumnName("statusvoznjeid");
            entity.Property(e => e.Troskoviid).HasColumnName("troskoviid");
            entity.Property(e => e.Voziloid).HasColumnName("voziloid");

            entity.HasOne(d => d.Lokacija).WithMany(p => p.Oglasvoznjas)
                .HasForeignKey(d => d.Lokacijaid)
                .HasConstraintName("oglasvoznja_lokacijaid_fkey");

            entity.HasOne(d => d.Statusvoznje).WithMany(p => p.Oglasvoznjas)
                .HasForeignKey(d => d.Statusvoznjeid)
                .HasConstraintName("oglasvoznja_statusvoznjeid_fkey");

            entity.HasOne(d => d.Troskovi).WithMany(p => p.Oglasvoznjas)
                .HasForeignKey(d => d.Troskoviid)
                .HasConstraintName("oglasvoznja_troskoviid_fkey");

            entity.HasOne(d => d.Vozilo).WithMany(p => p.Oglasvoznjas)
                .HasForeignKey(d => d.Voziloid)
                .HasConstraintName("oglasvoznja_voziloid_fkey");
        });

        modelBuilder.Entity<Poruka>(entity =>
        {
            entity.HasKey(e => e.Idporuka).HasName("poruka_pkey");

            entity.ToTable("poruka");

            entity.Property(e => e.Idporuka).HasColumnName("idporuka");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Korisnikvoznjaid).HasColumnName("korisnikvoznjaid");
            entity.Property(e => e.Putnikid).HasColumnName("putnikid");
            entity.Property(e => e.Vozacid).HasColumnName("vozacid");

            entity.HasOne(d => d.Korisnikvoznja).WithMany(p => p.Porukas)
                .HasForeignKey(d => d.Korisnikvoznjaid)
                .HasConstraintName("poruka_korisnikvoznjaid_fkey");

            entity.HasOne(d => d.Putnik).WithMany(p => p.PorukaPutniks)
                .HasForeignKey(d => d.Putnikid)
                .HasConstraintName("poruka_putnikid_fkey");

            entity.HasOne(d => d.Vozac).WithMany(p => p.PorukaVozacs)
                .HasForeignKey(d => d.Vozacid)
                .HasConstraintName("poruka_vozacid_fkey");
        });

        modelBuilder.Entity<Statusvoznje>(entity =>
        {
            entity.HasKey(e => e.Idstatusvoznje).HasName("statusvoznje_pkey");

            entity.ToTable("statusvoznje");

            entity.Property(e => e.Idstatusvoznje).HasColumnName("idstatusvoznje");
            entity.Property(e => e.Naziv)
                .HasMaxLength(50)
                .HasColumnName("naziv");
        });

        modelBuilder.Entity<Temp>(entity =>
        {
            entity.HasKey(e => e.Idkorisnik).HasName("temp_pkey");

            entity.ToTable("temp");

            entity.Property(e => e.Idkorisnik).HasColumnName("idkorisnik");
            entity.Property(e => e.Ime)
                .HasMaxLength(255)
                .HasColumnName("ime");
            entity.Property(e => e.Isconfirmed).HasColumnName("isconfirmed");
        });

        modelBuilder.Entity<Troskovi>(entity =>
        {
            entity.HasKey(e => e.Idtroskovi).HasName("troskovi_pkey");

            entity.ToTable("troskovi");

            entity.Property(e => e.Idtroskovi).HasColumnName("idtroskovi");
            entity.Property(e => e.Cestarina)
                .HasPrecision(10, 2)
                .HasColumnName("cestarina");
            entity.Property(e => e.Gorivo)
                .HasPrecision(10, 2)
                .HasColumnName("gorivo");
        });

        modelBuilder.Entity<Uloga>(entity =>
        {
            entity.HasKey(e => e.Iduloga).HasName("uloga_pkey");

            entity.ToTable("uloga");

            entity.Property(e => e.Iduloga).HasColumnName("iduloga");
            entity.Property(e => e.Naziv)
                .HasMaxLength(50)
                .HasColumnName("naziv");
        });

        modelBuilder.Entity<Vozilo>(entity =>
        {
            entity.HasKey(e => e.Idvozilo).HasName("vozilo_pkey");

            entity.ToTable("vozilo");

            entity.HasIndex(e => e.Registracija, "vozilo_registracija_key").IsUnique();

            entity.Property(e => e.Idvozilo).HasColumnName("idvozilo");
            entity.Property(e => e.Imageprometnaid).HasColumnName("imageprometnaid");
            entity.Property(e => e.Isconfirmed).HasColumnName("isconfirmed");
            entity.Property(e => e.Marka)
                .HasMaxLength(50)
                .HasColumnName("marka");
            entity.Property(e => e.Model)
                .HasMaxLength(50)
                .HasColumnName("model");
            entity.Property(e => e.Naziv)
                .HasMaxLength(50)
                .HasColumnName("naziv");
            entity.Property(e => e.Registracija)
                .HasMaxLength(20)
                .HasColumnName("registracija");
            entity.Property(e => e.Vozacid).HasColumnName("vozacid");

            entity.HasOne(d => d.Imageprometna).WithMany(p => p.Vozilos)
                .HasForeignKey(d => d.Imageprometnaid)
                .HasConstraintName("vozilo_imageprometnaid_fkey");

            entity.HasOne(d => d.Vozac).WithMany(p => p.Vozilos)
                .HasForeignKey(d => d.Vozacid)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("vozilo_vozacid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
