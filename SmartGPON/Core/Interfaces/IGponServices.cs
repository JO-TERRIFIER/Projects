// ============================================================
// SmartGPON — Core/Interfaces/IGponServices.cs — FRESH START
// ============================================================
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;

namespace SmartGPON.Core.Interfaces
{
    // ── Dashboard ───────────────────────────────────────────
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(int? clientId = null);
    }

    // dashboard VM lives here to avoid circular dependency
    public class DashboardViewModel
    {
        public int TotalProjets { get; set; }
        public int TotalZones { get; set; }
        public int TotalOlts { get; set; }
        public int TotalFdts { get; set; }
        public int TotalFats { get; set; }
        public int TotalBpis { get; set; }
        public int TotalFibres { get; set; }
        public int TotalChambres { get; set; }
        public int TotalSplitters { get; set; }
        public int TotalClients { get; set; }
    }

    // ── Security ────────────────────────────────────────────
    public interface ISecurityService
    {
        Task<SecurityDashboardData> GetSecurityDashboardAsync();
        Task<List<NetworkAlert>> GetAlertsAsync(int page = 1, int pageSize = 20);
        Task<List<TrafficCapture>> GetTrafficCapturesAsync(int? oltId = null, int page = 1, int pageSize = 20);
        Task<List<MaliciousOlt>> GetRogueOltsAsync();
        Task<List<SecurityEvent>> GetSecurityEventsAsync(int page = 1, int pageSize = 50);
        Task LogSecurityEventAsync(string description);
    }

    public class SecurityDashboardData
    {
        public int AlertCount { get; set; }
        public int RogueOltCount { get; set; }
        public int SimulationCount { get; set; }
        public int SecurityEventCount { get; set; }
        public List<NetworkAlert> RecentAlerts { get; set; } = new();
        public List<MaliciousOlt> RecentRogueOlts { get; set; } = new();
    }

    // ── Tree ────────────────────────────────────────────────
    public interface ITreeService
    {
        Task<NetworkTreeData> GetNetworkTreeAsync(int zoneId);
    }

    public class NetworkTreeData
    {
        public Zone Zone { get; set; } = null!;
        public List<Olt> Olts { get; set; } = new();
    }

    // ── Fibre ───────────────────────────────────────────────
    public interface IFibreService
    {
        Task<List<Fibre>> GetByZoneAsync(int zoneId);
        Task<Fibre?> GetByIdAsync(int id);
        Task<Fibre> CreateAsync(Fibre fibre);
        Task<Fibre> UpdateAsync(Fibre fibre);
        Task DeleteAsync(int id);
    }

    // ── Validation ──────────────────────────────────────────
    public interface IValidationService
    {
        Task<List<Validation>> GetByProjetAsync(int projetId);
        Task<Validation?> GetByIdAsync(int id);
        Task<Validation> CreateAsync(Validation validation, string userId);
        Task<Validation> UpdateAsync(Validation validation);
        Task DeleteAsync(int id);
    }

    // ── Splitter ────────────────────────────────────────────
    public interface ISplitterService
    {
        Task<List<Splitter>> GetByFdtAsync(int fdtId);
        Task<Splitter?> GetByIdAsync(int id);
        Task<Splitter> CreateAsync(Splitter splitter);
        Task<Splitter> UpdateAsync(Splitter splitter);
        Task DeleteAsync(int id);
    }

    // ── AttackSimulation ────────────────────────────────────
    public interface IAttackSimulationService
    {
        Task<List<AttackSimulation>> GetAllAsync(int page = 1, int pageSize = 20);
        Task<AttackSimulation?> GetByIdAsync(int id);
        Task<AttackSimulation> LaunchAsync(AttackSimulation simulation);
    }
}
