// ============================================================
// SmartGPON — Controllers/HomeController.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Core.Enums;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class HomeController : RbacControllerBase
    {
        private readonly IDashboardService _dashboard;
        private readonly ITreeService _tree;

        public HomeController(ApplicationDbContext db, IUserProjectAssignmentService assignments,
            IAuditLogService audit, IDashboardService dashboard, ITreeService tree)
            : base(db, assignments, audit)
        { _dashboard = dashboard; _tree = tree; }

        public async Task<IActionResult> Index()
        {
            int? clientId = null;
            if (IsVisiteur)
            {
                var user = await Db.Users.FindAsync(CurrentUserId);
                clientId = user?.ClientId;
            }
            var vm = await _dashboard.GetDashboardAsync(clientId);
            return View(vm);
        }

        public async Task<IActionResult> ArchitectureGpon(int? zoneId)
        {
            var accessibleIds = await AccessibleProjetIdsAsync();
            var zones = await Db.Zones.Where(z => accessibleIds.Contains(z.ProjetId))
                .Include(z => z.Projet).OrderBy(z => z.Nom).ToListAsync();
            ViewBag.Zones = zones;

            if (zoneId.HasValue)
            {
                // A1: Include(Projet) pour accéder à zone.Projet.ClientId sans NullRef
                var zone = await Db.Zones
                                   .Include(z => z.Projet)
                                   .FirstOrDefaultAsync(z => z.Id == zoneId.Value);
                ViewBag.ActiveZoneId   = zoneId.Value;
                ViewBag.ActiveProjetId = zone?.ProjetId ?? 0;
                ViewBag.ActiveClientId = zone?.Projet?.ClientId ?? 0;
                var treeData = await _tree.GetNetworkTreeAsync(zoneId.Value);
                return View(treeData);
            }
            return View();
        }

        // ── Endpoints JSON pour filtres en cascade Architecture GPON ──

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var ids = await AccessibleProjetIdsAsync();
            var data = await Db.Clients
                .Where(c => c.IsActive && Db.Projets.Any(p => p.ClientId == c.Id && ids.Contains(p.Id)))
                .OrderBy(c => c.Nom)
                .Select(c => new { id = c.Id, nom = c.Nom })   // M1: camelCase
                .ToListAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjets(int? clientId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Projets.Where(p => ids.Contains(p.Id)
                                       && p.Statut != ProjetStatut.Suspendu);
            if (clientId.HasValue && clientId.Value != 0)
                q = q.Where(p => p.ClientId == clientId.Value);
            return Json(await q.OrderBy(p => p.Nom)
                               .Select(p => new { id = p.Id, nom = p.Nom })  // M1
                               .ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> GetZones(int projetId)
        {
            var ids = await AccessibleProjetIdsAsync();
            if (!ids.Contains(projetId)) return Json(Array.Empty<object>());
            return Json(await Db.Zones
                .Where(z => z.ProjetId == projetId)
                .OrderBy(z => z.Nom)
                .Select(z => new { id = z.Id, nom = z.Nom })   // M1
                .ToListAsync());
        }


        public IActionResult Error() => View();
    }
}
