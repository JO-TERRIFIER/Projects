// ============================================================
// SmartGPON — Core/Entities/GponEntities.cs — FRESH START
// ============================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SmartGPON.Core.Enums;

namespace SmartGPON.Core.Entities
{
    // ── Client ──────────────────────────────────────────────
    public class Client
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        [Required, MaxLength(50)]  public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ICollection<Projet> Projets { get; set; } = new List<Projet>();
    }

    // ── Projet ──────────────────────────────────────────────
    public class Projet
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public ProjetStatut Statut { get; set; } = ProjetStatut.EnCours;

        public Client Client { get; set; } = null!;
        public ICollection<Zone> Zones { get; set; } = new List<Zone>();
        public ICollection<Validation> Validations { get; set; } = new List<Validation>();
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
        public ICollection<UserProjectAssignment> UserProjectAssignments { get; set; } = new List<UserProjectAssignment>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<ApprovalRequest> ApprovalRequests { get; set; } = new List<ApprovalRequest>();
    }

    // ── Zone ────────────────────────────────────────────────
    public class Zone
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Projet Projet { get; set; } = null!;
        public ICollection<Olt> Olts { get; set; } = new List<Olt>();
        public ICollection<Fibre> Fibres { get; set; } = new List<Fibre>();
        public ICollection<Chambre> Chambres { get; set; } = new List<Chambre>();
        public ICollection<Resource> Resources { get; set; } = new List<Resource>();
        public ICollection<UserProjectAssignment> UserProjectAssignments { get; set; } = new List<UserProjectAssignment>();
    }

    // ── OLT ─────────────────────────────────────────────────
    public class Olt
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(50)] public string? IpAddress { get; set; }
        public int NbrePorts { get; set; }

        public Zone Zone { get; set; } = null!;
        public ICollection<Fdt> Fdts { get; set; } = new List<Fdt>();
        public ICollection<AttackSimulation> AttackSimulations { get; set; } = new List<AttackSimulation>();
        public ICollection<MaliciousOlt> MaliciousOlts { get; set; } = new List<MaliciousOlt>();
        public ICollection<NetworkAlert> NetworkAlerts { get; set; } = new List<NetworkAlert>();
        public ICollection<TrafficCapture> TrafficCaptures { get; set; } = new List<TrafficCapture>();
    }

    // ── FDT ─────────────────────────────────────────────────
    public class Fdt
    {
        public int Id { get; set; }
        public int OltId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Olt Olt { get; set; } = null!;
        public ICollection<Splitter> Splitters { get; set; } = new List<Splitter>();
        public ICollection<Fat> Fats { get; set; } = new List<Fat>();
        public ICollection<Bpi> Bpis { get; set; } = new List<Bpi>();
    }

    // ── Splitter ────────────────────────────────────────────
    public class Splitter
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        public TypeSplitter TypeSplitter { get; set; }
        public int Quantite { get; set; }
        public bool IsActive { get; set; } = true;

        public Fdt Fdt { get; set; } = null!;
    }

    // ── FAT ─────────────────────────────────────────────────
    public class Fat
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Fdt Fdt { get; set; } = null!;
    }

    // ── BPI ─────────────────────────────────────────────────
    public class Bpi
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Fdt Fdt { get; set; } = null!;
        public ICollection<BoitierEtage> BoitiersEtage { get; set; } = new List<BoitierEtage>();
    }

    // ── BoitierEtage ─────────────────────────────────────────
    public class BoitierEtage
    {
        public int Id { get; set; }
        public int BpiId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Etage { get; set; }
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; } = true;

        public Bpi Bpi { get; set; } = null!;
    }

    // ── Fibre ───────────────────────────────────────────────
    public class Fibre
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public EquipementType SourceEquipementType { get; set; }
        public int SourceEquipementId { get; set; }
        public EquipementType CibleEquipementType { get; set; }
        public int CibleEquipementId { get; set; }
        public TypeFibre TypeFibre { get; set; }
        public decimal Longueur { get; set; }

        public Zone Zone { get; set; } = null!;
    }

    // ── Chambre ─────────────────────────────────────────────
    public class Chambre
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public Zone Zone { get; set; } = null!;
    }

    // ── Validation ──────────────────────────────────────────
    public class Validation
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        [Required, MaxLength(450)] public string UserId { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string TechNom { get; set; } = string.Empty;     // snapshot
        [Required, MaxLength(100)] public string TechPrenom { get; set; } = string.Empty;   // snapshot
        public EquipementType EquipementType { get; set; }
        public int EquipementId { get; set; }
        public ValidationStatut Statut { get; set; } = ValidationStatut.EnAttente;
        [MaxLength(500)] public string? Commentaire { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Projet Projet { get; set; } = null!;
    }

    // ── AuditLog ────────────────────────────────────────────
    public class AuditLog
    {
        public int Id { get; set; }
        [MaxLength(450)] public string? UserId { get; set; }
        public int? ProjetId { get; set; }
        [Required, MaxLength(100)] public string ActionType { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        [MaxLength(100)] public string? NomTech { get; set; }     // snapshot
        [MaxLength(100)] public string? PrenomTech { get; set; }  // snapshot
        [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public Projet? Projet { get; set; }
    }

    // ── Resource ────────────────────────────────────────────
    public class Resource
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        public int? ZoneId { get; set; }
        [Required, MaxLength(255)] public string NomFichier { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string CheminFichier { get; set; } = string.Empty;
        // Métadonnées upload (+5 colonnes P1)
        [Required, MaxLength(450)] public string UploadedByUserId { get; set; } = string.Empty;
        public DateTime UploadedAt    { get; set; } = DateTime.UtcNow;
        public long     FileSize      { get; set; }
        [Required, MaxLength(10)]  public string FileExtension { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string ContentType   { get; set; } = string.Empty;

        public Projet Projet { get; set; } = null!;
        public Zone? Zone { get; set; }
        public ICollection<DeletionRequest> DeletionRequests { get; set; } = new List<DeletionRequest>();
    }

    // ── DeletionRequest ──────────────────────────────────────
    public class DeletionRequest
    {
        public int Id { get; set; }
        public int ResourceId  { get; set; }
        [Required, MaxLength(450)] public string RequestedByUserId { get; set; } = string.Empty;
        public int ProjetId    { get; set; }
        public SmartGPON.Core.Enums.DeletionStatut Statut { get; set; } = SmartGPON.Core.Enums.DeletionStatut.EnAttente;
        public DateTime RequestedAt  { get; set; } = DateTime.UtcNow;
        [MaxLength(450)] public string? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt  { get; set; }
        [MaxLength(500)] public string? CommentaireRejet { get; set; }

        public Resource Resource { get; set; } = null!;
        public Projet   Projet   { get; set; } = null!;
    }

    // ── UserProjectAssignment ───────────────────────────────
    public class UserProjectAssignment
    {
        public int Id { get; set; }
        [Required, MaxLength(450)] public string UserId { get; set; } = string.Empty;
        public int ProjetId { get; set; }
        public AssignmentType AssignmentType { get; set; }
        public int? ZoneId { get; set; }   // TechTerrain only
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public Projet Projet { get; set; } = null!;
        public Zone? Zone { get; set; }
    }

    // ── ApprovalRequest ─────────────────────────────────────
    public class ApprovalRequest
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        [Required, MaxLength(450)] public string RequestedByUserId { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string TargetType { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string ActionType { get; set; } = string.Empty;
        public int Status { get; set; }  // Pending=0, Approved=1, Rejected=2

        public Projet Projet { get; set; } = null!;
    }

    // ── NetworkAlert ────────────────────────────────────────
    public class NetworkAlert
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        [Required, MaxLength(500)] public string Message { get; set; } = string.Empty;
        public AlertSeverite Severite { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public Olt? Olt { get; set; }
    }

    // ── SecurityEvent ───────────────────────────────────────
    public class SecurityEvent
    {
        public int Id { get; set; }
        [Required, MaxLength(1000)] public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    // ── TrafficCapture ──────────────────────────────────────
    public class TrafficCapture
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        public string? Data { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        public Olt? Olt { get; set; }
    }

    // ── MaliciousOlt ────────────────────────────────────────
    public class MaliciousOlt
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        public Olt? Olt { get; set; }
    }

    // ── AttackSimulation ────────────────────────────────────
    public class AttackSimulation
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        [Required, MaxLength(450)] public string LaunchedByUserId { get; set; } = string.Empty;
        public SimulationStatut Statut { get; set; } = SimulationStatut.EnAttente;
        public NiveauRisque NiveauRisque { get; set; } = NiveauRisque.Moyen;
        [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
        public string? ResultatDetails { get; set; }
        public DateTime DateLancement { get; set; } = DateTime.UtcNow;

        public Olt? Olt { get; set; }
    }
}