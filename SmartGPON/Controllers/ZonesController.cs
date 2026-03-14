// ============================================================
// SmartGPON — Controllers/ZonesController.cs — FRESH START
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
    public class ZonesController : RbacControllerBase
    {
        public ZonesController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? projetId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Zones.Where(z => ids.Contains(z.ProjetId)).Include(z => z.Projet).AsQueryable();
            if (projetId.HasValue) q = q.Where(z => z.ProjetId == projetId.Value);
            var zones = await q.OrderBy(z => z.Nom).Select(z => new ZoneDisplayVM
            {
                Id = z.Id, Nom = z.Nom, Latitude = z.Latitude, Longitude = z.Longitude,
                ProjetNom = z.Projet.Nom, ProjetId = z.ProjetId,
                OltCount = z.Olts.Count
            }).ToListAsync();
            ViewBag.ProjetId = projetId;
            return View(zones);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projetId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var ids = await AccessibleProjetIdsAsync();
            ViewBag.Projets = await Db.Projets.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nom).ToListAsync();
            return View(new ZoneCreateVM { ProjetId = projetId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ZoneCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.ProjetId)) return Forbid();
            if (!ModelState.IsValid)
            {
                var ids = await AccessibleProjetIdsAsync();
                ViewBag.Projets = await Db.Projets.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nom).ToListAsync();
                return View(vm);
            }
            var entity = new SmartGPON.Core.Entities.Zone
            {
                ProjetId = vm.ProjetId, Nom = vm.Nom, Latitude = vm.Latitude, Longitude = vm.Longitude
            };
            Db.Zones.Add(entity); await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Create", "Zone", entity.Id, $"Zone créée: {entity.Nom}");
            TempData["Success"] = "Zone créée.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var z = await Db.Zones.FindAsync(id);
            if (z == null) return NotFound();
            if (!await CanWriteAsync(z.ProjetId)) return Forbid();
            return View(new ZoneUpdateVM { Id = z.Id, ProjetId = z.ProjetId, Nom = z.Nom, Latitude = z.Latitude, Longitude = z.Longitude });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ZoneUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.ProjetId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var z = await Db.Zones.FindAsync(vm.Id);
            if (z == null) return NotFound();
            z.Nom = vm.Nom; z.Latitude = vm.Latitude; z.Longitude = vm.Longitude;
            await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Update", "Zone", z.Id, $"Zone modifiée: {z.Nom}");
            TempData["Success"] = "Zone modifiée.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var z = await Db.Zones.FindAsync(id);
            if (z == null) return NotFound();
            if (!await CanWriteAsync(z.ProjetId)) return Forbid();
            var pId = z.ProjetId;
            Db.Zones.Remove(z); await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "Zone", id, $"Zone supprimée: {z.Nom}");
            TempData["Success"] = "Zone supprimée.";
            return RedirectToAction(nameof(Index), new { projetId = pId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var z = await Db.Zones.Include(z => z.Projet).Include(z => z.Olts).FirstOrDefaultAsync(z => z.Id == id);
            if (z == null) return NotFound();
            return View(z);
        }
    }
}
