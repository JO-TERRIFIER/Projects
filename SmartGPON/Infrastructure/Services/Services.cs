// SmartGPON v3 - Infrastructure/Services.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private const string CACHE_KEY = "dashboard_stats";

        public DashboardService(ApplicationDbContext db, IMemoryCache cache)
        { _db = db; _cache = cache; }

        public async Task<DashboardViewModel> GetDashboardAsync(int? clientId = null)
        {
            var key = $"{CACHE_KEY}_{clientId}";
            if (_cache.TryGetValue(key, out DashboardViewModel? cached) && cached != null)
                return cached;

            var projetsQ = _db.Projets.AsNoTracking();
            if (clientId.HasValue) projetsQ = projetsQ.Where(p => p.ClientId == clientId);

            var vm = new DashboardViewModel
            {
                TotalProjets = await projetsQ.CountAsync(),
                ProjetsTermines = await projetsQ.CountAsync(p => p.Statut == ProjetStatut.Termine),
                ProjetsEnCours = await projetsQ.CountAsync(p => p.Statut == ProjetStatut.EnCours),
                ProjetsSuspendus = await projetsQ.CountAsync(p => p.Statut == ProjetStatut.Suspendu)
            };

            _cache.Set(key, vm, TimeSpan.FromMinutes(2));
            return vm;
        }
    }


    public class SecurityService : ISecurityService
    {
        private readonly ApplicationDbContext _db;
        public SecurityService(ApplicationDbContext db) { _db = db; }

        public async Task<SecurityDashboardViewModel> GetSecurityDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            return new SecurityDashboardViewModel
            {
                AlertesCritiques = await _db.NetworkAlerts.CountAsync(a => a.Severite == AlertSeverite.Critical && !a.IsRead),
                AlertesWarning = await _db.NetworkAlerts.CountAsync(a => a.Severite == AlertSeverite.Warning && !a.IsRead),
                RogueOltsActifs = await _db.MaliciousOlts.CountAsync(m => m.Statut == StatutMaliciousOlt.Actif),
                SimulationsEnCours = await _db.AttackSimulations.CountAsync(s => s.Statut == SimulationStatut.EnCours),
                EvenementsAujourdhui = await _db.SecurityEvents.CountAsync(e => e.DateEvenement >= today),
                RogueOlts = await _db.MaliciousOlts.AsNoTracking().Include(m => m.Olt).Where(m => m.Statut == StatutMaliciousOlt.Actif).OrderByDescending(m => m.DateDetection).Take(10).ToListAsync(),
                SimulationsRecentes = await _db.AttackSimulations.AsNoTracking().Include(s => s.Olt).OrderByDescending(s => s.DateLancement).Take(10).ToListAsync(),
                EvenementsRecents = await _db.SecurityEvents.AsNoTracking().OrderByDescending(e => e.DateEvenement).Take(20).ToListAsync()
            };
        }

        public async Task<int> LancerSimulationAsync(AttackSimulation sim)
        {
            sim.Statut = SimulationStatut.EnAttente;
            sim.DateLancement = DateTime.UtcNow;
            _db.AttackSimulations.Add(sim);
            await _db.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                var s = await _db.AttackSimulations.FindAsync(sim.Id);
                if (s == null) return;
                s.Statut = SimulationStatut.EnCours;
                await _db.SaveChangesAsync();

                await Task.Delay(new Random().Next(3000, 8000));
                s.Statut = SimulationStatut.Termine;
                s.DateFin = DateTime.UtcNow;
                s.ResultatDetails = JsonSerializer.Serialize(new
                {
                    PacketsSent = new Random().Next(100, 10000),
                    ResponseTime = new Random().Next(1, 500),
                    VulnerabilitiesFound = new Random().Next(0, 5),
                    AttackType = sim.TypeAttaque
                });
                _db.NetworkAlerts.Add(new NetworkAlert
                {
                    Titre = $"Simulation {sim.TypeAttaque} terminée",
                    Description = $"OLT ID {sim.OltId} - Niveau: {sim.NiveauRisque}",
                    Severite = sim.NiveauRisque >= NiveauRisque.Eleve ? AlertSeverite.Critical : AlertSeverite.Warning,
                    Type = "AttackSimulation",
                    OltId = sim.OltId,
                    DateAlerte = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            });

            return sim.Id;
        }

        public async Task<List<MaliciousOlt>> GetRogueOltsAsync() =>
            await _db.MaliciousOlts.AsNoTracking().Include(m => m.Olt).OrderByDescending(m => m.DateDetection).ToListAsync();

        public async Task<List<NetworkAlert>> GetAlertsAsync(bool unreadOnly = false, int page = 1, int pageSize = 20) =>
            await _db.NetworkAlerts.AsNoTracking()
                .Where(a => !unreadOnly || !a.IsRead)
                .OrderByDescending(a => a.DateAlerte)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public async Task MarkAlertReadAsync(int alertId)
        {
            var alert = await _db.NetworkAlerts.FindAsync(alertId);
            if (alert != null) { alert.IsRead = true; await _db.SaveChangesAsync(); }
        }

        public async Task<List<TrafficCapture>> GetTrafficCapturesAsync(int? oltId = null, int page = 1, int pageSize = 20) =>
            await _db.TrafficCaptures.AsNoTracking()
                .Include(t => t.Olt)
                .Where(t => !oltId.HasValue || t.OltId == oltId)
                .OrderByDescending(t => t.DateCapture)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

        public async Task LogSecurityEventAsync(string type, string description, string? ipSource = null, string? user = null, byte niveau = 1)
        {
            _db.SecurityEvents.Add(new SecurityEvent
            {
                Type = type,
                Description = description,
                IpSource = ipSource,
                Utilisateur = user,
                Niveau = niveau
            });
            await _db.SaveChangesAsync();
        }
    }

    public class TreeService : ITreeService
    {
        private readonly ApplicationDbContext _db;
        public TreeService(ApplicationDbContext db) { _db = db; }

        public async Task<NetworkTreeViewModel> GetNetworkTreeAsync(int zoneId)
        {
            var zone = await _db.Zones.AsNoTracking()
                .Include(z => z.Olts)
                    .ThenInclude(o => o.Fdts)
                        .ThenInclude(fd => fd.Fats)
                .Include(z => z.Olts)
                    .ThenInclude(o => o.Fdts)
                        .ThenInclude(fd => fd.Bpis)
                .FirstOrDefaultAsync(z => z.Id == zoneId);

            if (zone == null) return new NetworkTreeViewModel();

            return new NetworkTreeViewModel
            {
                ZoneId = zone.Id,
                ZoneNom = zone.Nom,
                Olts = zone.Olts.Select(o => new OltTreeNode
                {
                    Id = o.Id,
                    Nom = o.Nom,
                    IpAddress = o.IpAddress,
                    Statut = o.Statut,
                    Fdts = o.Fdts.Select(fd => new FdtTreeNode
                    {
                        Id = fd.Id,
                        Nom = fd.Nom,
                        NbSplitters1x8 = fd.NbSplitters1x8,
                        NbSplitters1x64 = fd.NbSplitters1x64,
                        Fats = fd.Fats.Select(fa => new FatTreeNode
                        {
                            Id = fa.Id,
                            Nom = fa.Nom,
                            Capacite = fa.Capacite
                        }).ToList(),
                        Bpis = fd.Bpis.Select(b => new BpiTreeNode
                        {
                            Id = b.Id,
                            Nom = b.Nom,
                            Capacite = b.Capacite,
                            NbSplitters1x8 = b.NbSplitters1x8
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<List<Zone>> GetZonesForClientAsync(int clientId) =>
            await _db.Zones.AsNoTracking().Include(z => z.Projet).Where(z => z.Projet.ClientId == clientId).OrderBy(z => z.Nom).ToListAsync();
    }
}
