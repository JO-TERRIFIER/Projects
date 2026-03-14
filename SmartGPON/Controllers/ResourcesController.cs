// ============================================================
// SmartGPON — Controllers/ResourcesController.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ResourcesController : RbacControllerBase
    {
        public ResourcesController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? projetId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Resources.Include(r => r.Projet).Include(r => r.Zone)
                .Where(r => ids.Contains(r.ProjetId));
            if (projetId.HasValue) q = q.Where(r => r.ProjetId == projetId.Value);
            var list = await q.OrderBy(r => r.NomFichier).Select(r => new ResourceDisplayVM
            {
                Id = r.Id, NomFichier = r.NomFichier, CheminFichier = r.CheminFichier,
                ProjetId = r.ProjetId, ZoneId = r.ZoneId,
                ProjetNom = r.Projet.Nom, ZoneNom = r.Zone != null ? r.Zone.Nom : null
            }).ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projetId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var ids = await AccessibleProjetIdsAsync();
            ViewBag.Projets = await Db.Projets.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nom).ToListAsync();
            return View(new ResourceCreateVM { ProjetId = projetId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.ProjetId)) return Forbid();
            if (!ModelState.IsValid)
            {
                var ids = await AccessibleProjetIdsAsync();
                ViewBag.Projets = await Db.Projets.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nom).ToListAsync();
                return View(vm);
            }
            var e = new SmartGPON.Core.Entities.Resource
            { ProjetId = vm.ProjetId, ZoneId = vm.ZoneId, NomFichier = vm.NomFichier, CheminFichier = vm.CheminFichier };
            Db.Resources.Add(e); await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Create", "Resource", e.Id, $"Ressource ajoutée: {e.NomFichier}");
            TempData["Success"] = "Ressource ajoutée.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var r = await Db.Resources.FindAsync(id);
            if (r == null) return NotFound();
            if (!await CanWriteAsync(r.ProjetId)) return Forbid();
            var pId = r.ProjetId;
            Db.Resources.Remove(r); await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "Resource", id, $"Ressource supprimée: {r.NomFichier}");
            TempData["Success"] = "Ressource supprimée.";
            return RedirectToAction(nameof(Index), new { projetId = pId });
        }
    }
}
