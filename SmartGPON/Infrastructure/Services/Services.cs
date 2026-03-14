// ============================================================
// SmartGPON — Infrastructure/Services/Services.cs — FRESH START
// ============================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Infrastructure.Services
{
    // ── DashboardService ────────────────────────────────────
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DashboardService> _log;

        public DashboardService(ApplicationDbContext db, ILogger<DashboardService> log)
        { _db = db; _log = log; }

        public async Task<DashboardViewModel> GetDashboardAsync(int? clientId = null)
        {
            try
            {
                var projetsQ = _db.Projets.AsQueryable();
                if (clientId.HasValue) projetsQ = projetsQ.Where(p => p.ClientId == clientId.Value);
                var projetIds = await projetsQ.Select(p => p.Id).ToListAsync();

                var zonesQ = _db.Zones.Where(z => projetIds.Contains(z.ProjetId));
                var zoneIds = await zonesQ.Select(z => z.Id).ToListAsync();

                return new DashboardViewModel
                {
                    TotalClients = clientId.HasValue ? 1 : await _db.Clients.CountAsync(),
                    TotalProjets = projetIds.Count,
                    TotalZones = zoneIds.Count,
                    TotalOlts = await _db.Olts.CountAsync(o => zoneIds.Contains(o.ZoneId)),
                    TotalFdts = await _db.Fdts.CountAsync(f => _db.Olts.Where(o => zoneIds.Contains(o.ZoneId)).Select(o => o.Id).Contains(f.OltId)),
                    TotalFats = await _db.Fats.CountAsync(),
                    TotalBpis = await _db.Bpis.CountAsync(),
                    TotalFibres = await _db.Fibres.CountAsync(f => zoneIds.Contains(f.ZoneId)),
                    TotalChambres = await _db.Chambres.CountAsync(c => zoneIds.Contains(c.ZoneId)),
                    TotalSplitters = await _db.Splitters.CountAsync()
                };
            }
            catch (Exception ex) { _log.LogError(ex, "GetDashboardAsync error"); throw; }
        }
    }

    // ── SecurityService ─────────────────────────────────────
    public class SecurityService : ISecurityService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SecurityService> _log;

        public SecurityService(ApplicationDbContext db, ILogger<SecurityService> log)
        { _db = db; _log = log; }

        public async Task<SecurityDashboardData> GetSecurityDashboardAsync()
        {
            try
            {
                return new SecurityDashboardData
                {
                    AlertCount = await _db.NetworkAlerts.CountAsync(),
                    RogueOltCount = await _db.MaliciousOlts.CountAsync(),
                    SimulationCount = await _db.AttackSimulations.CountAsync(),
                    SecurityEventCount = await _db.SecurityEvents.CountAsync(),
                    RecentAlerts = await _db.NetworkAlerts.OrderByDescending(a => a.OccurredAt).Take(10).Include(a => a.Olt).ToListAsync(),
                    RecentRogueOlts = await _db.MaliciousOlts.OrderByDescending(m => m.DetectedAt).Take(10).Include(m => m.Olt).ToListAsync()
                };
            }
            catch (Exception ex) { _log.LogError(ex, "GetSecurityDashboardAsync error"); throw; }
        }

        public async Task<List<NetworkAlert>> GetAlertsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _db.NetworkAlerts.Include(a => a.Olt)
                    .OrderByDescending(a => a.OccurredAt)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "GetAlertsAsync error"); throw; }
        }

        public async Task<List<TrafficCapture>> GetTrafficCapturesAsync(int? oltId = null, int page = 1, int pageSize = 20)
        {
            try
            {
                var q = _db.TrafficCaptures.Include(t => t.Olt).AsQueryable();
                if (oltId.HasValue) q = q.Where(t => t.OltId == oltId.Value);
                return await q.OrderByDescending(t => t.CapturedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "GetTrafficCapturesAsync error"); throw; }
        }

        public async Task<List<MaliciousOlt>> GetRogueOltsAsync()
        {
            try { return await _db.MaliciousOlts.Include(m => m.Olt).OrderByDescending(m => m.DetectedAt).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetRogueOltsAsync error"); throw; }
        }

        public async Task<List<SecurityEvent>> GetSecurityEventsAsync(int page = 1, int pageSize = 50)
        {
            try
            {
                return await _db.SecurityEvents.OrderByDescending(s => s.OccurredAt)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "GetSecurityEventsAsync error"); throw; }
        }

        public async Task LogSecurityEventAsync(string description)
        {
            try
            {
                _db.SecurityEvents.Add(new SecurityEvent { Description = description });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "LogSecurityEventAsync error"); throw; }
        }
    }

    // ── TreeService ─────────────────────────────────────────
    public class TreeService : ITreeService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TreeService> _log;

        public TreeService(ApplicationDbContext db, ILogger<TreeService> log)
        { _db = db; _log = log; }

        public async Task<NetworkTreeData> GetNetworkTreeAsync(int zoneId)
        {
            try
            {
                var zone = await _db.Zones.FindAsync(zoneId);
                if (zone == null) throw new KeyNotFoundException($"Zone {zoneId} not found");

                var olts = await _db.Olts
                    .Where(o => o.ZoneId == zoneId)
                    .Include(o => o.Fdts).ThenInclude(f => f.Splitters)
                    .Include(o => o.Fdts).ThenInclude(f => f.Fats)
                    .Include(o => o.Fdts).ThenInclude(f => f.Bpis)
                    .ToListAsync();

                return new NetworkTreeData { Zone = zone, Olts = olts };
            }
            catch (Exception ex) { _log.LogError(ex, "GetNetworkTreeAsync error"); throw; }
        }
    }

    // ── FibreService ────────────────────────────────────────
    public class FibreService : IFibreService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FibreService> _log;

        public FibreService(ApplicationDbContext db, ILogger<FibreService> log)
        { _db = db; _log = log; }

        public async Task<List<Fibre>> GetByZoneAsync(int zoneId)
        {
            try { return await _db.Fibres.Where(f => f.ZoneId == zoneId).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetByZoneAsync error"); throw; }
        }

        public async Task<Fibre?> GetByIdAsync(int id)
        {
            try { return await _db.Fibres.FindAsync(id); }
            catch (Exception ex) { _log.LogError(ex, "GetByIdAsync error"); throw; }
        }

        public async Task<Fibre> CreateAsync(Fibre fibre)
        {
            try { _db.Fibres.Add(fibre); await _db.SaveChangesAsync(); return fibre; }
            catch (Exception ex) { _log.LogError(ex, "CreateAsync error"); throw; }
        }

        public async Task<Fibre> UpdateAsync(Fibre fibre)
        {
            try { _db.Fibres.Update(fibre); await _db.SaveChangesAsync(); return fibre; }
            catch (Exception ex) { _log.LogError(ex, "UpdateAsync error"); throw; }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var e = await _db.Fibres.FindAsync(id);
                if (e != null) { _db.Fibres.Remove(e); await _db.SaveChangesAsync(); }
            }
            catch (Exception ex) { _log.LogError(ex, "DeleteAsync error"); throw; }
        }
    }

    // ── ValidationService ───────────────────────────────────
    public class ValidationService : IValidationService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ValidationService> _log;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

        public ValidationService(ApplicationDbContext db, ILogger<ValidationService> log,
            Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        { _db = db; _log = log; _userManager = userManager; }

        public async Task<List<Validation>> GetByProjetAsync(int projetId)
        {
            try { return await _db.Validations.Where(v => v.ProjetId == projetId).OrderByDescending(v => v.CreatedAt).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetByProjetAsync error"); throw; }
        }

        public async Task<Validation?> GetByIdAsync(int id)
        {
            try { return await _db.Validations.FindAsync(id); }
            catch (Exception ex) { _log.LogError(ex, "GetByIdAsync error"); throw; }
        }

        public async Task<Validation> CreateAsync(Validation validation, string userId)
        {
            try
            {
                // Snapshot TechNom/TechPrenom from user
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    validation.UserId = userId;
                    validation.TechNom = user.LastName;
                    validation.TechPrenom = user.FirstName;
                }
                _db.Validations.Add(validation);
                await _db.SaveChangesAsync();
                return validation;
            }
            catch (Exception ex) { _log.LogError(ex, "CreateAsync error"); throw; }
        }

        public async Task<Validation> UpdateAsync(Validation validation)
        {
            try { _db.Validations.Update(validation); await _db.SaveChangesAsync(); return validation; }
            catch (Exception ex) { _log.LogError(ex, "UpdateAsync error"); throw; }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var e = await _db.Validations.FindAsync(id);
                if (e != null) { _db.Validations.Remove(e); await _db.SaveChangesAsync(); }
            }
            catch (Exception ex) { _log.LogError(ex, "DeleteAsync error"); throw; }
        }
    }

    // ── SplitterService ─────────────────────────────────────
    public class SplitterService : ISplitterService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SplitterService> _log;

        public SplitterService(ApplicationDbContext db, ILogger<SplitterService> log)
        { _db = db; _log = log; }

        public async Task<List<Splitter>> GetByFdtAsync(int fdtId)
        {
            try { return await _db.Splitters.Where(s => s.FdtId == fdtId).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetByFdtAsync error"); throw; }
        }

        public async Task<Splitter?> GetByIdAsync(int id)
        {
            try { return await _db.Splitters.FindAsync(id); }
            catch (Exception ex) { _log.LogError(ex, "GetByIdAsync error"); throw; }
        }

        public async Task<Splitter> CreateAsync(Splitter splitter)
        {
            try { _db.Splitters.Add(splitter); await _db.SaveChangesAsync(); return splitter; }
            catch (Exception ex) { _log.LogError(ex, "CreateAsync error"); throw; }
        }

        public async Task<Splitter> UpdateAsync(Splitter splitter)
        {
            try { _db.Splitters.Update(splitter); await _db.SaveChangesAsync(); return splitter; }
            catch (Exception ex) { _log.LogError(ex, "UpdateAsync error"); throw; }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var e = await _db.Splitters.FindAsync(id);
                if (e != null) { _db.Splitters.Remove(e); await _db.SaveChangesAsync(); }
            }
            catch (Exception ex) { _log.LogError(ex, "DeleteAsync error"); throw; }
        }
    }

    // ── AttackSimulationService ─────────────────────────────
    public class AttackSimulationService : IAttackSimulationService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AttackSimulationService> _log;
        private readonly IHttpContextAccessor _httpCtx;

        public AttackSimulationService(ApplicationDbContext db, ILogger<AttackSimulationService> log, IHttpContextAccessor httpCtx)
        { _db = db; _log = log; _httpCtx = httpCtx; }

        public async Task<List<AttackSimulation>> GetAllAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                return await _db.AttackSimulations.Include(a => a.Olt)
                    .OrderByDescending(a => a.DateLancement)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "GetAllAsync error"); throw; }
        }

        public async Task<AttackSimulation?> GetByIdAsync(int id)
        {
            try { return await _db.AttackSimulations.Include(a => a.Olt).FirstOrDefaultAsync(a => a.Id == id); }
            catch (Exception ex) { _log.LogError(ex, "GetByIdAsync error"); throw; }
        }

        public async Task<AttackSimulation> LaunchAsync(AttackSimulation simulation)
        {
            try
            {
                // LaunchedByUserId from HttpContext
                var userId = _httpCtx.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId)) simulation.LaunchedByUserId = userId;

                simulation.Statut = SimulationStatut.EnCours;
                simulation.DateLancement = DateTime.UtcNow;
                _db.AttackSimulations.Add(simulation);
                await _db.SaveChangesAsync();
                return simulation;
            }
            catch (Exception ex) { _log.LogError(ex, "LaunchAsync error"); throw; }
        }
    }
}
