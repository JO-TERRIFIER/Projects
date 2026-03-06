// SmartGPON v3 – Infrastructure/Data/ApplicationDbContext.cs
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

        // GPON
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Projet> Projets => Set<Projet>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Olt> Olts => Set<Olt>();
        public DbSet<Fdt> Fdts => Set<Fdt>();
        public DbSet<Fat> Fats => Set<Fat>();
        public DbSet<Splitter> Splitters => Set<Splitter>();
        public DbSet<Ont> Onts => Set<Ont>();
        public DbSet<Fibre> Fibres => Set<Fibre>();
        public DbSet<Chambre> Chambres => Set<Chambre>();
        public DbSet<Technicien> Techniciens => Set<Technicien>();
        public DbSet<Test> Tests => Set<Test>();
        public DbSet<Validation> Validations => Set<Validation>();

        // Security
        public DbSet<AttackSimulation> AttackSimulations => Set<AttackSimulation>();
        public DbSet<MaliciousOlt> MaliciousOlts => Set<MaliciousOlt>();
        public DbSet<TrafficCapture> TrafficCaptures => Set<TrafficCapture>();
        public DbSet<NetworkAlert> NetworkAlerts => Set<NetworkAlert>();
        public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Indexes for performance
            builder.Entity<Projet>().HasIndex(e => e.ClientId);
            builder.Entity<Zone>().HasIndex(e => e.ProjetId);
            builder.Entity<Olt>().HasIndex(e => e.ZoneId);
            builder.Entity<Fdt>().HasIndex(e => e.OltId);
            builder.Entity<Fat>().HasIndex(e => e.FdtId);
            builder.Entity<Splitter>().HasIndex(e => e.FatId);
            builder.Entity<Ont>().HasIndex(e => e.SplitterId);
            builder.Entity<Ont>().HasIndex(e => e.SerialNumber).IsUnique();
            builder.Entity<NetworkAlert>().HasIndex(e => e.DateAlerte);
            builder.Entity<NetworkAlert>().HasIndex(e => e.IsRead);
            builder.Entity<AttackSimulation>().HasIndex(e => e.DateLancement);
            builder.Entity<TrafficCapture>().HasIndex(e => e.DateCapture);
            builder.Entity<SecurityEvent>().HasIndex(e => e.DateEvenement);

            // Cascade rules
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
            builder.Entity<Splitter>().HasOne(e => e.Fat).WithMany(e => e.Splitters)
                .HasForeignKey(e => e.FatId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Ont>().HasOne(e => e.Splitter).WithMany(e => e.Onts)
                .HasForeignKey(e => e.SplitterId).OnDelete(DeleteBehavior.Cascade);
            builder.Entity<Technicien>().HasOne(e => e.Client).WithMany(e => e.Techniciens)
                .HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Restrict);

            // Decimal precision
            builder.Entity<Zone>().Property(e => e.Latitude).HasPrecision(10, 7);
            builder.Entity<Zone>().Property(e => e.Longitude).HasPrecision(10, 7);
            builder.Entity<Ont>().Property(e => e.SignalRx).HasPrecision(6, 2);
            builder.Entity<Ont>().Property(e => e.SignalTx).HasPrecision(6, 2);
        }
    }
}
