using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly ISecurityService _sec;
        private readonly ApplicationDbContext _db;

        public SecurityController(ISecurityService sec, ApplicationDbContext db)
        {
            _sec = sec;
            _db = db;
        }

        public async Task<IActionResult> Index(int page = 1, string filter = "All")
        {
            var vm = await _sec.GetSecurityDashboardAsync();
            ViewBag.Filter = filter;
            const int pageSize = 15;
            
            var query = _db.NetworkAlerts.Include(a => a.Olt).AsNoTracking();
            query = filter switch
            {
                "Critical" => query.Where(a => a.Severite == AlertSeverite.Critical),
                "Warning" => query.Where(a => a.Severite == AlertSeverite.Warning),
                "Info" => query.Where(a => a.Severite == AlertSeverite.Info),
                "Unread" => query.Where(a => !a.IsRead),
                _ => query
            };
            
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.DateAlerte).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            
            vm.AlertesPaginees = new PagedResult<NetworkAlert> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
            
            return View(vm);
        }

        [HttpPost, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> MarkRead(int id, string actionDescription)
        {
            if (string.IsNullOrWhiteSpace(actionDescription)) actionDescription = "Marquer alerte lue";
            await _sec.MarkAlertReadAsync(id);
            return Ok();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> MarkAllRead(string actionDescription)
        {
            if (string.IsNullOrWhiteSpace(actionDescription)) actionDescription = "Tout marquer lu";
            var unread = await _db.NetworkAlerts.Where(a => !a.IsRead).ToListAsync();
            unread.ForEach(a => a.IsRead = true);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Alertes marquées comme lues.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur")]
        public async Task<IActionResult> ResolveRogue(int id, string actionDescription)
        {
            if (string.IsNullOrWhiteSpace(actionDescription)) actionDescription = "Résolution rogue";
            var rogue = await _db.MaliciousOlts.FindAsync(id);
            if (rogue != null)
            {
                rogue.Statut = StatutMaliciousOlt.Resolu;
                rogue.DateResolution = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            TempData["Success"] = "Rogue OLT marqué résolu.";
            return RedirectToAction(nameof(RogueOlts));
        }

        public async Task<IActionResult> RogueOlts() => View(await _sec.GetRogueOltsAsync());
        public async Task<IActionResult> TrafficCaptures(int? oltId = null, int page = 1) => View(await _sec.GetTrafficCapturesAsync(oltId, page));
        public async Task<IActionResult> Simulations() => View(await _db.AttackSimulations.AsNoTracking().Include(s => s.Olt).OrderByDescending(s => s.DateLancement).Take(50).ToListAsync());

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> LancerSimulation()
        {
            ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
            return View(new SimulationFormViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> LancerSimulation(SimulationFormViewModel vm, string actionDescription)
        {
            if (string.IsNullOrWhiteSpace(actionDescription)) actionDescription = "Lancement simulation";
            if (!ModelState.IsValid)
            {
                ViewBag.Olts = await _db.Olts.AsNoTracking().OrderBy(o => o.Nom).ToListAsync();
                return View(vm);
            }

            var sim = new AttackSimulation
            {
                OltId = vm.OltId,
                TypeAttaque = vm.TypeAttaque,
                NiveauRisque = vm.NiveauRisque,
                Parametres = vm.Parametres,
                LancePar = User.Identity?.Name
            };
            var simId = await _sec.LancerSimulationAsync(sim);
            TempData["Success"] = $"Simulation #{simId} lancée.";
            return RedirectToAction(nameof(Simulations));
        }

        public async Task<IActionResult> SimulationDetails(int id) => View(await _db.AttackSimulations.AsNoTracking().Include(s => s.Olt).FirstOrDefaultAsync(s => s.Id == id));
    }
}

