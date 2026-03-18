// ============================================================
// SmartGPON — ViewModels/ViewModels.cs — FRESH START
// ============================================================
using System.ComponentModel.DataAnnotations;
using SmartGPON.Core.Enums;

namespace SmartGPON.Web.ViewModels
{
    // ═══════════════════════════════════════════════════════════
    // CLIENT
    // ═══════════════════════════════════════════════════════════
    public class ClientCreateVM
    {
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        [Required, MaxLength(50)]  public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
    public class ClientUpdateVM
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        [Required, MaxLength(50)]  public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
    public class ClientDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ProjetCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // PROJET
    // ═══════════════════════════════════════════════════════════
    public class ProjetCreateVM
    {
        public int ClientId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public ProjetStatut Statut { get; set; } = ProjetStatut.EnCours;
    }
    public class ProjetUpdateVM
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public ProjetStatut Statut { get; set; }
    }
    public class ProjetDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public ProjetStatut Statut { get; set; }
        public string ClientNom { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public int ZoneCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // ZONE
    // ═══════════════════════════════════════════════════════════
    public class ZoneCreateVM
    {
        public int ProjetId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class ZoneUpdateVM
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class ZoneDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string ProjetNom { get; set; } = string.Empty;
        public int ProjetId { get; set; }
        public int OltCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // OLT
    // ═══════════════════════════════════════════════════════════
    public class OltCreateVM
    {
        public int ZoneId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(50)] public string? IpAddress { get; set; }
        public int NbrePorts { get; set; }
    }
    public class OltUpdateVM
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(50)] public string? IpAddress { get; set; }
        public int NbrePorts { get; set; }
    }
    public class OltDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public int NbrePorts { get; set; }
        public string ZoneNom { get; set; } = string.Empty;
        public int ZoneId { get; set; }
        public int FdtCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // FDT
    // ═══════════════════════════════════════════════════════════
    public class FdtCreateVM
    {
        public int OltId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class FdtUpdateVM
    {
        public int Id { get; set; }
        public int OltId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class FdtDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string OltNom { get; set; } = string.Empty;
        public int OltId { get; set; }
        public int SplitterCount { get; set; }
        public int FatCount { get; set; }
        public int BpiCount { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // SPLITTER
    // ═══════════════════════════════════════════════════════════
    public class SplitterCreateVM
    {
        public int FdtId { get; set; }
        [Required] public TypeSplitter TypeSplitter { get; set; }
        [Required] public int Quantite { get; set; }
    }
    public class SplitterUpdateVM
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        [Required] public TypeSplitter TypeSplitter { get; set; }
        [Required] public int Quantite { get; set; }
        public bool IsActive { get; set; }
    }
    public class SplitterDisplayVM
    {
        public int Id { get; set; }
        public TypeSplitter TypeSplitter { get; set; }
        public int Quantite { get; set; }
        public bool IsActive { get; set; }
        public string FdtNom { get; set; } = string.Empty;
        public int FdtId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // FAT
    // ═══════════════════════════════════════════════════════════
    public class FatCreateVM
    {
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class FatUpdateVM
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class FatDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string FdtNom { get; set; } = string.Empty;
        public int FdtId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // BPI
    // ═══════════════════════════════════════════════════════════
    public class BpiCreateVM
    {
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class BpiUpdateVM
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
    public class BpiDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string FdtNom { get; set; } = string.Empty;
        public int FdtId { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // BOITIER ETAGE
    // ═══════════════════════════════════════════════════════════
    public class BoitierEtageCreateVM
    {
        [Required] public int BpiId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [Required, Range(0, 99)] public int Etage { get; set; }
        [Required, Range(1, 999)] public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class BoitierEtageUpdateVM
    {
        public int Id { get; set; }
        [Required] public int BpiId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [Required, Range(0, 99)] public int Etage { get; set; }
        [Required, Range(1, 999)] public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class BoitierEtageDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int BpiId { get; set; }
        public string BpiNom { get; set; } = string.Empty;
        public int Etage { get; set; }
        public int Capacite { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool IsActive { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // FIBRE
    // ═══════════════════════════════════════════════════════════
    public class FibreCreateVM
    {
        public int ZoneId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        [Required] public EquipementType SourceEquipementType { get; set; }
        public int SourceEquipementId { get; set; }
        [Required] public EquipementType CibleEquipementType { get; set; }
        public int CibleEquipementId { get; set; }
        [Required] public TypeFibre TypeFibre { get; set; }
        public decimal Longueur { get; set; }
    }
    public class FibreUpdateVM
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(200)] public string Nom { get; set; } = string.Empty;
        [Required] public EquipementType SourceEquipementType { get; set; }
        public int SourceEquipementId { get; set; }
        [Required] public EquipementType CibleEquipementType { get; set; }
        public int CibleEquipementId { get; set; }
        [Required] public TypeFibre TypeFibre { get; set; }
        public decimal Longueur { get; set; }
    }
    public class FibreDisplayVM
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public EquipementType SourceEquipementType { get; set; }
        public int SourceEquipementId { get; set; }
        public EquipementType CibleEquipementType { get; set; }
        public int CibleEquipementId { get; set; }
        public TypeFibre TypeFibre { get; set; }
        public decimal Longueur { get; set; }
        public int ZoneId { get; set; }
        public string ZoneNom { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // CHAMBRE
    // ═══════════════════════════════════════════════════════════
    public class ChambreCreateVM
    {
        public int ZoneId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
    public class ChambreUpdateVM
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
    public class ChambreDisplayVM
    {
        public int Id { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int ZoneId { get; set; }
        public string ZoneNom { get; set; } = string.Empty;
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════
    public class ValidationCreateVM
    {
        public int ProjetId { get; set; }
        [Required] public EquipementType EquipementType { get; set; }
        public int EquipementId { get; set; }
        public ValidationStatut Statut { get; set; } = ValidationStatut.EnAttente;
        [MaxLength(500)] public string? Commentaire { get; set; }
    }
    public class ValidationUpdateVM
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        public ValidationStatut Statut { get; set; }
        [MaxLength(500)] public string? Commentaire { get; set; }
    }
    public class ValidationDisplayVM
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TechNom { get; set; } = string.Empty;      // snapshot
        public string TechPrenom { get; set; } = string.Empty;    // snapshot
        public EquipementType EquipementType { get; set; }
        public int EquipementId { get; set; }
        public ValidationStatut Statut { get; set; }
        public string? Commentaire { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // AUDIT LOG
    // ═══════════════════════════════════════════════════════════
    public class AuditLogDisplayVM
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public int? ProjetId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? NomTech { get; set; }       // snapshot
        public string? PrenomTech { get; set; }     // snapshot
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // RESOURCE
    // ═══════════════════════════════════════════════════════════
    public class ResourceCreateVM
    {
        public int ProjetId { get; set; }
        public int? ZoneId { get; set; }
        [Required, MaxLength(255)] public string NomFichier { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string CheminFichier { get; set; } = string.Empty;
    }
    public class ResourceUpdateVM
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        public int? ZoneId { get; set; }
        [Required, MaxLength(255)] public string NomFichier { get; set; } = string.Empty;
        [Required, MaxLength(500)] public string CheminFichier { get; set; } = string.Empty;
    }
    public class ResourceDisplayVM
    {
        public int Id { get; set; }
        public string NomFichier { get; set; } = string.Empty;
        public string CheminFichier { get; set; } = string.Empty;
        public int ProjetId { get; set; }
        public int? ZoneId { get; set; }
        public string ProjetNom { get; set; } = string.Empty;
        public string? ZoneNom { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // APPROVAL REQUEST
    // ═══════════════════════════════════════════════════════════
    public class ApprovalRequestDisplayVM
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        public string ProjetNom { get; set; } = string.Empty;
        public string RequestedByUserId { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public int Status { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // ATTACK SIMULATION
    // ═══════════════════════════════════════════════════════════
    public class AttackSimulationCreateVM
    {
        public int? OltId { get; set; }
        [Required, MaxLength(2000)] public string Description { get; set; } = string.Empty;
        public NiveauRisque NiveauRisque { get; set; } = NiveauRisque.Moyen;
        // A1 — champs injectés par le controller (non modifiables par l'utilisateur)
        public string LaunchedByUserId { get; set; } = string.Empty;
        public SimulationStatut Statut { get; set; } = SimulationStatut.EnAttente;
        [MaxLength(2000)] public string? ResultatDetails { get; set; }
        public DateTime DateLancement { get; set; }
    }
    public class AttackSimulationDisplayVM
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        public string? OltNom { get; set; }
        public string LaunchedByUserId { get; set; } = string.Empty;
        public SimulationStatut Statut { get; set; }
        public NiveauRisque NiveauRisque { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ResultatDetails { get; set; }
        public DateTime DateLancement { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // NETWORK ALERT
    // ═══════════════════════════════════════════════════════════
    public class NetworkAlertDisplayVM
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        public string? OltNom { get; set; }
        public string Message { get; set; } = string.Empty;
        public AlertSeverite Severite { get; set; }
        public DateTime OccurredAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // MALICIOUS OLT
    // ═══════════════════════════════════════════════════════════
    public class MaliciousOltDisplayVM
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        public string? OltNom { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // USER PROJECT ASSIGNMENT
    // ═══════════════════════════════════════════════════════════
    public class UserProjectAssignmentCreateVM
    {
        [Required] public string UserId { get; set; } = string.Empty;
        public int ProjetId { get; set; }
        [Required] public AssignmentType AssignmentType { get; set; }
        public int? ZoneId { get; set; }
    }
    public class UserProjectAssignmentDisplayVM
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int ProjetId { get; set; }
        public string ProjetNom { get; set; } = string.Empty;
        public AssignmentType AssignmentType { get; set; }
        public int? ZoneId { get; set; }
        public string? ZoneNom { get; set; }
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    // ═══════════════════════════════════════════════════════════
    // RESOURCE — FILE UPLOAD SYSTEM (P4)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Upload d'un nouveau fichier.</summary>
    public class ResourceUploadVM
    {
        public int ProjetId { get; set; }
        public int? ZoneId  { get; set; }
        [Required] public IFormFile File { get; set; } = null!;
    }

    /// <summary>Renommage d'un fichier existant (Edit A5).</summary>
    public class ResourceRenameVM
    {
        public int Id       { get; set; }
        public int ProjetId { get; set; }
        [Required, MaxLength(255)] public string NomFichier { get; set; } = string.Empty;
    }

    /// <summary>Affichage d'un fichier dans _FileList.</summary>
    public class ResourceFileVM
    {
        public int      Id              { get; set; }
        public int      ProjetId        { get; set; }
        public int?     ZoneId          { get; set; }
        public string   NomFichier      { get; set; } = string.Empty;
        public string   FileExtension   { get; set; } = string.Empty;
        public string   ContentType     { get; set; } = string.Empty;
        public long     FileSize        { get; set; }
        public string?  UploadedByNom   { get; set; }
        public DateTime UploadedAt      { get; set; }
        // Droits calculés (jamais de valeur numérique enum)
        public bool CanDeleteDirect    { get; set; }
        public bool CanRequestDelete   { get; set; }
        public bool HasPendingDeletion { get; set; }
    }

    /// <summary>Demande de suppression en attente (PendingDeletions).</summary>
    public class DeletionRequestVM
    {
        public int      Id             { get; set; }
        public int      ResourceId     { get; set; }
        public int      ProjetId       { get; set; }
        public string   NomFichier     { get; set; } = string.Empty;
        public string   RequestedByNom { get; set; } = string.Empty;
        public DateTime RequestedAt    { get; set; }
        public string?  CommentaireRejet { get; set; }
    }

    /// <summary>Page de détail d'un Projet avec fichiers (P8).</summary>
    public class ProjetDetailsVM
    {
        public int    Id        { get; set; }
        public string Nom       { get; set; } = string.Empty;
        public string ClientNom { get; set; } = string.Empty;
        public SmartGPON.Core.Enums.ProjetStatut Statut { get; set; }
        public IEnumerable<ResourceFileVM>    Files            { get; set; } = Enumerable.Empty<ResourceFileVM>();
        public IEnumerable<DeletionRequestVM> PendingDeletions { get; set; } = Enumerable.Empty<DeletionRequestVM>();
        public bool CanUpload { get; set; }
        public bool CanReview { get; set; }
    }

    /// <summary>Page de détail d'une Zone avec fichiers (P8).</summary>
    public class ZoneDetailsVM
    {
        public int      Id        { get; set; }
        public string   Nom       { get; set; } = string.Empty;
        public string   ProjetNom { get; set; } = string.Empty;
        public int      ProjetId  { get; set; }
        public decimal? Latitude  { get; set; }
        public decimal? Longitude { get; set; }
        public IEnumerable<ResourceFileVM>    Files            { get; set; } = Enumerable.Empty<ResourceFileVM>();
        public IEnumerable<DeletionRequestVM> PendingDeletions { get; set; } = Enumerable.Empty<DeletionRequestVM>();
        public bool CanUpload { get; set; }
        public bool CanReview { get; set; }
    }
}
