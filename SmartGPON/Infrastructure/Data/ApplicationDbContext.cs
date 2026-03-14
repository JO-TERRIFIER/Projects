// ============================================================
// SmartGPON — Infrastructure/Data/ApplicationDbContext.cs — FRESH START
// ============================================================
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;

namespace SmartGPON.Infrastructure.Data
{
    // ── ApplicationUser ─────────────────────────────────────
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
        [MaxLength(100)] public string? Specialite { get; set; }   // TechTerrain/TechDessin only
        public int? ClientId { get; set; }                          // Visiteur only
        public bool IsActive { get; set; } = true;

        public Client? Client { get; set; }
    }

    // ── DbContext ───────────────────────────────────────────
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // GPON topology
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Projet> Projets => Set<Projet>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Olt> Olts => Set<Olt>();
        public DbSet<Fdt> Fdts => Set<Fdt>();
        public DbSet<Splitter> Splitters => Set<Splitter>();
        public DbSet<Fat> Fats => Set<Fat>();
        public DbSet<Bpi> Bpis => Set<Bpi>();
        public DbSet<BoitierEtage> BoitiersEtage => Set<BoitierEtage>();
        public DbSet<Fibre> Fibres => Set<Fibre>();
        public DbSet<Chambre> Chambres => Set<Chambre>();

        // Workflow
        public DbSet<Validation> Validations => Set<Validation>();
        public DbSet<Resource> Resources => Set<Resource>();
        public DbSet<UserProjectAssignment> UserProjectAssignments => Set<UserProjectAssignment>();
        public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // Security
        public DbSet<NetworkAlert> NetworkAlerts => Set<NetworkAlert>();
        public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
        public DbSet<TrafficCapture> TrafficCaptures => Set<TrafficCapture>();
        public DbSet<MaliciousOlt> MaliciousOlts => Set<MaliciousOlt>();
        public DbSet<AttackSimulation> AttackSimulations => Set<AttackSimulation>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ── ApplicationUser FK ──────────────────────────
            b.Entity<ApplicationUser>()
                .HasOne(u => u.Client)
                .WithMany()
                .HasForeignKey(u => u.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Client ──────────────────────────────────────
            b.Entity<Client>().HasIndex(e => e.Code).IsUnique();

            // ── Projet ──────────────────────────────────────
            b.Entity<Projet>().HasIndex(e => e.ClientId);
            b.Entity<Projet>().Property(e => e.Statut).HasConversion<byte>();
            b.Entity<Projet>()
                .HasOne(e => e.Client).WithMany(e => e.Projets)
                .HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);

            // ── Zone ────────────────────────────────────────
            b.Entity<Zone>().HasIndex(e => e.ProjetId);
            b.Entity<Zone>().Property(e => e.Latitude).HasPrecision(9, 6);
            b.Entity<Zone>().Property(e => e.Longitude).HasPrecision(9, 6);
            b.Entity<Zone>()
                .HasOne(e => e.Projet).WithMany(e => e.Zones)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Cascade);

            // ── OLT ─────────────────────────────────────────
            b.Entity<Olt>().HasIndex(e => e.ZoneId);
            b.Entity<Olt>()
                .HasOne(e => e.Zone).WithMany(e => e.Olts)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.Cascade);

            // ── FDT ─────────────────────────────────────────
            b.Entity<Fdt>().HasIndex(e => e.OltId);
            b.Entity<Fdt>().Property(e => e.Latitude).HasPrecision(9, 6);
            b.Entity<Fdt>().Property(e => e.Longitude).HasPrecision(9, 6);
            b.Entity<Fdt>()
                .HasOne(e => e.Olt).WithMany(e => e.Fdts)
                .HasForeignKey(e => e.OltId).OnDelete(DeleteBehavior.Cascade);

            // ── Splitter ────────────────────────────────────
            b.Entity<Splitter>().HasIndex(e => e.FdtId);
            b.Entity<Splitter>().Property(e => e.TypeSplitter).HasConversion<byte>();
            b.Entity<Splitter>()
                .HasOne(e => e.Fdt).WithMany(e => e.Splitters)
                .HasForeignKey(e => e.FdtId).OnDelete(DeleteBehavior.Cascade);

            // ── FAT ─────────────────────────────────────────
            b.Entity<Fat>().HasIndex(e => e.FdtId);
            b.Entity<Fat>().Property(e => e.Latitude).HasPrecision(9, 6);
            b.Entity<Fat>().Property(e => e.Longitude).HasPrecision(9, 6);
            b.Entity<Fat>()
                .HasOne(e => e.Fdt).WithMany(e => e.Fats)
                .HasForeignKey(e => e.FdtId).OnDelete(DeleteBehavior.Cascade);

            // ── BPI ─────────────────────────────────────────
            b.Entity<Bpi>().HasIndex(e => e.FdtId);
            b.Entity<Bpi>().Property(e => e.Latitude).HasPrecision(9, 6);
            b.Entity<Bpi>().Property(e => e.Longitude).HasPrecision(9, 6);
            b.Entity<Bpi>()
                .HasOne(e => e.Fdt).WithMany(e => e.Bpis)
                .HasForeignKey(e => e.FdtId).OnDelete(DeleteBehavior.Cascade);

            // ── BoitierEtage ─────────────────────────────────
            b.Entity<BoitierEtage>().HasKey(e => e.Id);
            b.Entity<BoitierEtage>().HasIndex(e => e.BpiId).HasDatabaseName("IX_BoitierEtage_BpiId");
            b.Entity<BoitierEtage>().Property(e => e.Nom).IsRequired().HasMaxLength(100);
            b.Entity<BoitierEtage>().Property(e => e.Latitude).HasPrecision(9, 6);
            b.Entity<BoitierEtage>().Property(e => e.Longitude).HasPrecision(9, 6);
            b.Entity<BoitierEtage>()
                .HasOne(e => e.Bpi).WithMany(e => e.BoitiersEtage)
                .HasForeignKey(e => e.BpiId).OnDelete(DeleteBehavior.Cascade);

            // ── Fibre ───────────────────────────────────────
            b.Entity<Fibre>().HasIndex(e => e.ZoneId);
            b.Entity<Fibre>().Property(e => e.SourceEquipementType).HasConversion<byte>();
            b.Entity<Fibre>().Property(e => e.CibleEquipementType).HasConversion<byte>();
            b.Entity<Fibre>().Property(e => e.TypeFibre).HasConversion<byte>();
            b.Entity<Fibre>().Property(e => e.Longueur).HasPrecision(10, 2);
            b.Entity<Fibre>()
                .HasOne(e => e.Zone).WithMany(e => e.Fibres)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.Cascade);

            // ── Chambre ─────────────────────────────────────
            b.Entity<Chambre>().HasIndex(e => e.ZoneId);
            b.Entity<Chambre>().Property(e => e.Latitude).HasPrecision(9, 6);
            b.Entity<Chambre>().Property(e => e.Longitude).HasPrecision(9, 6);
            b.Entity<Chambre>()
                .HasOne(e => e.Zone).WithMany(e => e.Chambres)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.Cascade);

            // ── Validation ──────────────────────────────────
            b.Entity<Validation>().HasIndex(e => e.ProjetId);
            b.Entity<Validation>().Property(e => e.Statut).HasConversion<byte>();
            b.Entity<Validation>().Property(e => e.EquipementType).HasConversion<byte>();
            b.Entity<Validation>()
                .HasOne(e => e.Projet).WithMany(e => e.Validations)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Cascade);

            // ── AuditLog ────────────────────────────────────
            b.Entity<AuditLog>().HasIndex(e => new { e.ProjetId, e.OccurredAt });
            b.Entity<AuditLog>()
                .HasOne(e => e.Projet).WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.SetNull);

            // ── Resource ────────────────────────────────────
            b.Entity<Resource>().HasIndex(e => e.ProjetId);
            b.Entity<Resource>().HasIndex(e => e.ZoneId);
            b.Entity<Resource>()
                .HasOne(e => e.Projet).WithMany(e => e.Resources)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<Resource>()
                .HasOne(e => e.Zone).WithMany(e => e.Resources)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.ClientSetNull);

            // ── UserProjectAssignment ───────────────────────
            b.Entity<UserProjectAssignment>()
                .HasIndex(e => new { e.UserId, e.ProjetId, e.AssignmentType }).IsUnique();
            b.Entity<UserProjectAssignment>().HasIndex(e => e.ProjetId);
            b.Entity<UserProjectAssignment>().Property(e => e.AssignmentType).HasConversion<byte>();
            b.Entity<UserProjectAssignment>()
                .HasOne(e => e.Projet).WithMany(e => e.UserProjectAssignments)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Cascade);
            b.Entity<UserProjectAssignment>()
                .HasOne(e => e.Zone).WithMany(e => e.UserProjectAssignments)
                .HasForeignKey(e => e.ZoneId).OnDelete(DeleteBehavior.ClientSetNull);

            // ── ApprovalRequest ─────────────────────────────
            b.Entity<ApprovalRequest>().HasIndex(e => new { e.ProjetId, e.Status });
            b.Entity<ApprovalRequest>()
                .HasOne(e => e.Projet).WithMany(e => e.ApprovalRequests)
                .HasForeignKey(e => e.ProjetId).OnDelete(DeleteBehavior.Cascade);

            // ── NetworkAlert ────────────────────────────────
            b.Entity<NetworkAlert>().HasIndex(e => e.OccurredAt);
            b.Entity<NetworkAlert>().Property(e => e.Severite).HasConversion<byte>();
            b.Entity<NetworkAlert>()
                .HasOne(e => e.Olt).WithMany(e => e.NetworkAlerts)
                .HasForeignKey(e => e.OltId).OnDelete(DeleteBehavior.SetNull);

            // ── SecurityEvent ───────────────────────────────
            b.Entity<SecurityEvent>().HasIndex(e => e.OccurredAt);

            // ── TrafficCapture ──────────────────────────────
            b.Entity<TrafficCapture>().HasIndex(e => e.CapturedAt);
            b.Entity<TrafficCapture>()
                .HasOne(e => e.Olt).WithMany(e => e.TrafficCaptures)
                .HasForeignKey(e => e.OltId).OnDelete(DeleteBehavior.SetNull);

            // ── MaliciousOlt ────────────────────────────────
            b.Entity<MaliciousOlt>()
                .HasOne(e => e.Olt).WithMany(e => e.MaliciousOlts)
                .HasForeignKey(e => e.OltId).OnDelete(DeleteBehavior.SetNull);

            // ── AttackSimulation ─────────────────────────────
            b.Entity<AttackSimulation>().HasIndex(e => e.DateLancement);
            b.Entity<AttackSimulation>().Property(e => e.Statut).HasConversion<byte>();
            b.Entity<AttackSimulation>().Property(e => e.NiveauRisque).HasConversion<byte>();
            b.Entity<AttackSimulation>()
                .HasOne(e => e.Olt).WithMany(e => e.AttackSimulations)
                .HasForeignKey(e => e.OltId).OnDelete(DeleteBehavior.SetNull);
        }
    }
}
