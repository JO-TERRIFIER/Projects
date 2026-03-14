// ============================================================
// SmartGPON — Controllers/HomeController.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

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
                var treeData = await _tree.GetNetworkTreeAsync(zoneId.Value);
                return View(treeData);
            }
            return View();
        }


        public IActionResult Error() => View();
    }
}
