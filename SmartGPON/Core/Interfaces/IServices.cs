// SmartGPON v3 – Core/Interfaces/IServices.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartGPON.Core.Entities;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Core.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(int? clientId = null);
    }

    public interface ISecurityService
    {
        Task<SecurityDashboardViewModel> GetSecurityDashboardAsync();
        Task<int> LancerSimulationAsync(AttackSimulation simulation);
        Task<List<MaliciousOlt>> GetRogueOltsAsync();
        Task<List<NetworkAlert>> GetAlertsAsync(bool unreadOnly = false, int page = 1, int pageSize = 20);
        Task MarkAlertReadAsync(int alertId);
        Task<List<TrafficCapture>> GetTrafficCapturesAsync(int? oltId = null, int page = 1, int pageSize = 20);
        Task LogSecurityEventAsync(string type, string description, string? ipSource = null, string? user = null, byte niveau = 1);
    }

    public interface ISupervisionService
    {
        Task<List<Olt>> GetOltsAsync(int? zoneId = null);
        Task<OltCapacityViewModel> GetOltCapacityAsync(int oltId);
        Task<List<Ont>> GetOntsAlarmesAsync();
        Task UpdateOntStatutAsync(int ontId, int statut);
    }

    public interface ITreeService
    {
        Task<NetworkTreeViewModel> GetNetworkTreeAsync(int zoneId);
        Task<List<Zone>> GetZonesForClientAsync(int clientId);
    }
}
