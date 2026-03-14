// ============================================================
// SmartGPON — Controllers/BoitiersEtageController.cs
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
    public class BoitiersEtageController : RbacControllerBase
    {
        public BoitiersEtageController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        // ── Index ─────────────────────────────────────────────
        public async Task<IActionResult> Index(int? bpiId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.BoitiersEtage
                .Include(e => e.Bpi).ThenInclude(b => b.Fdt).ThenInclude(f => f.Olt).ThenInclude(o => o.Zone)
                .Where(e => ids.Contains(e.Bpi.Fdt.Olt.Zone.ProjetId));
            if (bpiId.HasValue) q = q.Where(e => e.BpiId == bpiId.Value);
            var list = await q.OrderBy(e => e.Etage).ThenBy(e => e.Nom)
                .Select(e => new BoitierEtageDisplayVM
                {
                    Id = e.Id, Nom = e.Nom, BpiId = e.BpiId, BpiNom = e.Bpi.Nom,
                    Etage = e.Etage, Capacite = e.Capacite,
                    Latitude = e.Latitude, Longitude = e.Longitude, IsActive = e.IsActive
                }).ToListAsync();
            ViewBag.BpiId = bpiId;
            return View(list);
        }

        // ── Details ───────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var e = await Db.BoitiersEtage
                .Include(be => be.Bpi)
                .FirstOrDefaultAsync(be => be.Id == id);
            if (e == null) return NotFound();
            var vm = new BoitierEtageDisplayVM
            {
                Id = e.Id, Nom = e.Nom, BpiId = e.BpiId, BpiNom = e.Bpi.Nom,
                Etage = e.Etage, Capacite = e.Capacite,
                Latitude = e.Latitude, Longitude = e.Longitude, IsActive = e.IsActive
            };
            return View(vm);
        }

        // ── Create GET ────────────────────────────────────────
        [HttpGet]
        public IActionResult Create(int? bpiId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            return View(new BoitierEtageCreateVM { BpiId = bpiId ?? 0 });
        }

        // ── Create POST ───────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BoitierEtageCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var pId = await ProjetIdFromBpiAsync(vm.BpiId);
            if (!await CanWriteAsync(pId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var entity = new SmartGPON.Core.Entities.BoitierEtage
            {
                BpiId = vm.BpiId, Nom = vm.Nom, Etage = vm.Etage, Capacite = vm.Capacite,
                Latitude = vm.Latitude, Longitude = vm.Longitude, IsActive = vm.IsActive
            };
            Db.BoitiersEtage.Add(entity);
            await Db.SaveChangesAsync();
            await LogAsync(pId, "Create", "BoitierEtage", entity.Id, $"Boîtier d'étage créé: {entity.Nom}");
            TempData["Success"] = "Boîtier d'étage créé.";
            return RedirectToAction(nameof(Index), new { bpiId = vm.BpiId });
        }

        // ── Edit GET ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var e = await Db.BoitiersEtage.FindAsync(id);
            if (e == null) return NotFound();
            if (!await CanWriteAsync(await ProjetIdFromBpiAsync(e.BpiId))) return Forbid();
            return View(new BoitierEtageUpdateVM
            {
                Id = e.Id, BpiId = e.BpiId, Nom = e.Nom, Etage = e.Etage, Capacite = e.Capacite,
                Latitude = e.Latitude, Longitude = e.Longitude, IsActive = e.IsActive
            });
        }

        // ── Edit POST ─────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BoitierEtageUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(await ProjetIdFromBpiAsync(vm.BpiId))) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var e = await Db.BoitiersEtage.FindAsync(vm.Id);
            if (e == null) return NotFound();
            e.Nom = vm.Nom; e.Etage = vm.Etage; e.Capacite = vm.Capacite;
            e.Latitude = vm.Latitude; e.Longitude = vm.Longitude; e.IsActive = vm.IsActive;
            await Db.SaveChangesAsync();
            await LogAsync(await ProjetIdFromBpiAsync(vm.BpiId), "Update", "BoitierEtage", e.Id, $"Boîtier d'étage modifié: {e.Nom}");
            TempData["Success"] = "Boîtier d'étage modifié.";
            return RedirectToAction(nameof(Index), new { bpiId = vm.BpiId });
        }

        // ── Delete POST ───────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var e = await Db.BoitiersEtage.FindAsync(id);
            if (e == null) return NotFound();
            var pId = await ProjetIdFromBpiAsync(e.BpiId);
            if (!await CanWriteAsync(pId)) return Forbid();
            var bId = e.BpiId;
            var nom = e.Nom;
            Db.BoitiersEtage.Remove(e);
            await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "BoitierEtage", id, $"Boîtier d'étage supprimé: {nom}");
            TempData["Success"] = "Boîtier d'étage supprimé.";
            return RedirectToAction(nameof(Index), new { bpiId = bId });
        }
    }
}
