// ============================================================
// SmartGPON v3 – Core/Entities/GponEntities.cs
// ============================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartGPON.Core.Enums;

namespace SmartGPON.Core.Entities
{
    public class Client
    {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [Required, MaxLength(20)]  public string Code { get; set; } = string.Empty;
        [MaxLength(200)] public string? Adresse { get; set; }
        [MaxLength(20)]  public string? Telephone { get; set; }
        [MaxLength(100)] public string? Email { get; set; }
        [MaxLength(300)] public string? Logo { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public ICollection<Projet> Projets { get; set; } = new List<Projet>();
        public ICollection<Technicien> Techniciens { get; set; } = new List<Technicien>();
    }

    public class Projet
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        [Required, MaxLength(150)] public string Nom { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
        public ProjetStatut Statut { get; set; } = ProjetStatut.EnCours;
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public Client Client { get; set; } = null!;
        public ICollection<Zone> Zones { get; set; } = new List<Zone>();
        public ICollection<Validation> Validations { get; set; } = new List<Validation>();
    }

    public class Zone
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(300)] public string? Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public Projet Projet { get; set; } = null!;
        public ICollection<Olt> Olts { get; set; } = new List<Olt>();
        public ICollection<Fibre> Fibres { get; set; } = new List<Fibre>();
        public ICollection<Chambre> Chambres { get; set; } = new List<Chambre>();
    }

    public class Olt
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(50)]  public string? Marque { get; set; }
        [MaxLength(50)]  public string? Modele { get; set; }
        [MaxLength(45)]  public string? IpAddress { get; set; }
        public int NbrePorts { get; set; } = 16;
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public DateTime? DateInstall { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public Zone Zone { get; set; } = null!;
        public ICollection<Fdt> Fdts { get; set; } = new List<Fdt>();
        public ICollection<AttackSimulation> AttackSimulations { get; set; } = new List<AttackSimulation>();
        public ICollection<MaliciousOlt> MaliciousOlts { get; set; } = new List<MaliciousOlt>();
        public ICollection<NetworkAlert> NetworkAlerts { get; set; } = new List<NetworkAlert>();
    }

    public class Fdt
    {
        public int Id { get; set; }
        public int OltId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; } = 8;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public Olt Olt { get; set; } = null!;
        public ICollection<Fat> Fats { get; set; } = new List<Fat>();
    }

    public class Fat
    {
        public int Id { get; set; }
        public int FdtId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; } = 8;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public Fdt Fdt { get; set; } = null!;
        public ICollection<Splitter> Splitters { get; set; } = new List<Splitter>();
    }

    public class Splitter
    {
        public int Id { get; set; }
        public int FatId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(10)] public string Ratio { get; set; } = "1:8";
        public int NombreSorties { get; set; } = 8;
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public Fat Fat { get; set; } = null!;
        public ICollection<Ont> Onts { get; set; } = new List<Ont>();
    }

    public class Ont
    {
        public int Id { get; set; }
        public int SplitterId { get; set; }
        [Required, MaxLength(50)]  public string SerialNumber { get; set; } = string.Empty;
        [MaxLength(100)] public string? Nom { get; set; }
        [MaxLength(17)]  public string? MacAddress { get; set; }
        [MaxLength(45)]  public string? IpAddress { get; set; }
        [MaxLength(50)]  public string? Modele { get; set; }
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public decimal? SignalRx { get; set; }
        public decimal? SignalTx { get; set; }
        public DateTime? DateInstall { get; set; }
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public Splitter Splitter { get; set; } = null!;
        public ICollection<Test> Tests { get; set; } = new List<Test>();
    }

    public class Fibre
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        public decimal? Longueur { get; set; }
        [MaxLength(30)] public string? Type { get; set; }
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public Zone Zone { get; set; } = null!;
    }

    public class Chambre
    {
        public int Id { get; set; }
        public int ZoneId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(30)] public string? Type { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public StatutEquipement Statut { get; set; } = StatutEquipement.Actif;
        public Zone Zone { get; set; } = null!;
    }

    public class Technicien
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        [Required, MaxLength(100)] public string Nom { get; set; } = string.Empty;
        [MaxLength(100)] public string? Prenom { get; set; }
        [MaxLength(150)] public string? Email { get; set; }
        [MaxLength(20)]  public string? Telephone { get; set; }
        [MaxLength(100)] public string? Specialite { get; set; }
        public bool IsActive { get; set; } = true;
        public Client Client { get; set; } = null!;
        public ICollection<Test> Tests { get; set; } = new List<Test>();
    }

    public class Test
    {
        public int Id { get; set; }
        public int OntId { get; set; }
        public int? TechnicienId { get; set; }
        [MaxLength(50)] public string? TypeTest { get; set; }
        public ResultatTest Resultat { get; set; } = ResultatTest.EnCours;
        [MaxLength(500)] public string? Notes { get; set; }
        public DateTime DateTest { get; set; } = DateTime.UtcNow;
        public Ont Ont { get; set; } = null!;
        public Technicien? Technicien { get; set; }
    }

    public class Validation
    {
        public int Id { get; set; }
        public int ProjetId { get; set; }
        public int? TechnicienId { get; set; }
        public StatutValidation Statut { get; set; } = StatutValidation.EnAttente;
        [MaxLength(500)] public string? Commentaire { get; set; }
        public DateTime DateValidation { get; set; } = DateTime.UtcNow;
        public Projet Projet { get; set; } = null!;
        public Technicien? Technicien { get; set; }
    }

    // ── SECURITY ENTITIES ────────────────────────────────────

    public class AttackSimulation
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        [Required, MaxLength(50)] public string TypeAttaque { get; set; } = string.Empty;
        public string? Parametres { get; set; }
        public SimulationStatut Statut { get; set; } = SimulationStatut.EnAttente;
        public NiveauRisque NiveauRisque { get; set; } = NiveauRisque.Moyen;
        public string? ResultatDetails { get; set; }
        [MaxLength(100)] public string? LancePar { get; set; }
        public DateTime DateLancement { get; set; } = DateTime.UtcNow;
        public DateTime? DateFin { get; set; }
        public Olt? Olt { get; set; }
    }

    public class MaliciousOlt
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        [MaxLength(45)]  public string? IpSuspecte { get; set; }
        [MaxLength(17)]  public string? MacSuspecte { get; set; }
        [MaxLength(200)] public string? RaisonDetection { get; set; }
        public int NiveauConfiance { get; set; } = 70;
        public StatutMaliciousOlt Statut { get; set; } = StatutMaliciousOlt.Actif;
        public DateTime DateDetection { get; set; } = DateTime.UtcNow;
        public DateTime? DateResolution { get; set; }
        public Olt? Olt { get; set; }
    }

    public class TrafficCapture
    {
        public int Id { get; set; }
        public int? OltId { get; set; }
        public long TailleOctets { get; set; }
        public int NombrePaquets { get; set; }
        [MaxLength(30)] public string? Protocole { get; set; }
        [MaxLength(45)] public string? IpSource { get; set; }
        [MaxLength(45)] public string? IpDestination { get; set; }
        public bool AnomalieDetectee { get; set; }
        [MaxLength(100)] public string? TypeAnomalie { get; set; }
        public DateTime DateCapture { get; set; } = DateTime.UtcNow;
        public Olt? Olt { get; set; }
    }

    public class NetworkAlert
    {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Titre { get; set; } = string.Empty;
        [MaxLength(1000)] public string? Description { get; set; }
        public AlertSeverite Severite { get; set; } = AlertSeverite.Warning;
        [MaxLength(50)] public string? Type { get; set; }
        public int? OltId { get; set; }
        public bool IsRead { get; set; }
        public DateTime DateAlerte { get; set; } = DateTime.UtcNow;
        public Olt? Olt { get; set; }
    }

    public class SecurityEvent
    {
        public int Id { get; set; }
        [Required, MaxLength(50)] public string Type { get; set; } = string.Empty;
        [MaxLength(500)] public string? Description { get; set; }
        [MaxLength(45)]  public string? IpSource { get; set; }
        [MaxLength(100)] public string? Utilisateur { get; set; }
        public byte Niveau { get; set; } = 1;
        public DateTime DateEvenement { get; set; } = DateTime.UtcNow;
    }
}
