// SmartGPON v3 - Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;

namespace SmartGPON.Infrastructure.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int? ClientId { get; set; }
        public Client? Client { get; set; }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Projet> Projets => Set<Projet>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Olt> Olts => Set<Olt>();
        public DbSet<Fdt> Fdts => Set<Fdt>();
        public DbSet<Fat> Fats => Set<Fat>();
        public DbSet<Bpi> Bpis => Set<Bpi>();
        public DbSet<Fibre> Fibres => Set<Fibre>();
        public DbSet<Chambre> Chambres => Set<Chambre>();
        public DbSet<Technicien> Techniciens => Set<Technicien>();
        public DbSet<Validation> Validations => Set<Validation>();
        public DbSet<Resource> Resources => Set<Resource>();

        public DbSet<AttackSimulation> AttackSimulations => Set<AttackSimulation>();
        public DbSet<MaliciousOlt> MaliciousOlts => Set<MaliciousOlt>();
        public DbSet<TrafficCapture> TrafficCaptures => Set<TrafficCapture>();
        public DbSet<NetworkAlert> NetworkAlerts => Set<NetworkAlert>();
        public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Projet>().HasIndex(e => e.ClientId);
            builder.Entity<Zone>().HasIndex(e => e.ProjetId);
            builder.Entity<Olt>().HasIndex(e => e.ZoneId);
            builder.Entity<Fdt>().HasIndex(e => e.OltId);
            builder.Entity<Fat>().HasIndex(e => e.FdtId);
            builder.Entity<Bpi>().HasIndex(e => e.FdtId);
            builder.Entity<Resource>().HasIndex(e => e.ZoneId);
            builder.Entity<Resource>().HasIndex(e => e.ProjetId);
            builder.Entity<NetworkAlert>().HasIndex(e => e.DateAlerte);
            builder.Entity<NetworkAlert>().HasIndex(e => e.IsRead);
            builder.Entity<AttackSimulation>().HasIndex(e => e.DateLancement);
            builder.Entity<TrafficCapture>().HasIndex(e => e.DateCapture);
            builder.Entity<SecurityEvent>().HasIndex(e => e.DateEvenement);

            builder.Entity<Projet>().HasOne(e => e.Client).WithMany(e => e.Projets)
                .HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Zone>().HasOne(e => e.Projet).WithMany(e => e.Zones)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Olt>().HasOne(e => e.Zone).WithMany(e => e.Olts)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Fdt>().HasOne(e => e.Olt).WithMany(e => e.Fdts)
                .HasForeignKey(e => e.OltId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Fat>().HasOne(e => e.Fdt).WithMany(e => e.Fats)
                .HasForeignKey(e => e.FdtId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Bpi>().HasOne(e => e.Fdt).WithMany(e => e.Bpis)
                .HasForeignKey(e => e.FdtId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Resource>().HasOne(e => e.Zone).WithMany(e => e.Resources)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Resource>().HasOne(e => e.Projet).WithMany(e => e.Resources)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.ClientSetNull);
            builder.Entity<Technicien>().HasOne(e => e.Projet).WithMany(e => e.Techniciens)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Zone>().Property(e => e.Latitude).HasPrecision(10, 7);
            builder.Entity<Zone>().Property(e => e.Longitude).HasPrecision(10, 7);
            builder.Entity<Bpi>().Property(e => e.Latitude).HasPrecision(10, 7);
            builder.Entity<Bpi>().Property(e => e.Longitude).HasPrecision(10, 7);
            builder.Entity<Fat>().Property(e => e.Latitude).HasPrecision(10, 7);
            builder.Entity<Fat>().Property(e => e.Longitude).HasPrecision(10, 7);
            builder.Entity<Fdt>().Property(e => e.Latitude).HasPrecision(10, 7);
            builder.Entity<Fdt>().Property(e => e.Longitude).HasPrecision(10, 7);
            builder.Entity<Chambre>().Property(e => e.Latitude).HasPrecision(10, 7);
            builder.Entity<Chambre>().Property(e => e.Longitude).HasPrecision(10, 7);
            builder.Entity<Fibre>().Property(e => e.Longueur).HasPrecision(8, 2);
        }
    }
}
