// ============================================================
// SmartGPON — Controllers/FatsController.cs — FRESH START
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
    public class FatsController : RbacControllerBase
    {
        public FatsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? fdtId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Fats.Include(f => f.Fdt).ThenInclude(f => f.Olt).ThenInclude(o => o.Zone)
                .Where(f => ids.Contains(f.Fdt.Olt.Zone.ProjetId));
            if (fdtId.HasValue) q = q.Where(f => f.FdtId == fdtId.Value);
            var list = await q.OrderBy(f => f.Nom).Select(f => new FatDisplayVM
            {
                Id = f.Id, Nom = f.Nom, Capacite = f.Capacite, Latitude = f.Latitude, Longitude = f.Longitude,
                FdtNom = f.Fdt.Nom, FdtId = f.FdtId
            }).ToListAsync();
            ViewBag.FdtId = fdtId;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create(int? fdtId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            return View(new FatCreateVM { FdtId = fdtId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FatCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var pId = await ProjetIdFromFdtAsync(vm.FdtId);
            if (!await CanWriteAsync(pId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var e = new SmartGPON.Core.Entities.Fat
            { FdtId = vm.FdtId, Nom = vm.Nom, Capacite = vm.Capacite, Latitude = vm.Latitude, Longitude = vm.Longitude };
            Db.Fats.Add(e); await Db.SaveChangesAsync();
            await LogAsync(pId, "Create", "FAT", e.Id, $"FAT créé: {e.Nom}");
            TempData["Success"] = "FAT créé.";
            return RedirectToAction(nameof(Index), new { fdtId = vm.FdtId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var f = await Db.Fats.FindAsync(id);
            if (f == null) return NotFound();
            if (!await CanWriteAsync(await ProjetIdFromFdtAsync(f.FdtId))) return Forbid();
            return View(new FatUpdateVM { Id = f.Id, FdtId = f.FdtId, Nom = f.Nom, Capacite = f.Capacite, Latitude = f.Latitude, Longitude = f.Longitude });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FatUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(await ProjetIdFromFdtAsync(vm.FdtId))) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var f = await Db.Fats.FindAsync(vm.Id);
            if (f == null) return NotFound();
            f.Nom = vm.Nom; f.Capacite = vm.Capacite; f.Latitude = vm.Latitude; f.Longitude = vm.Longitude;
            await Db.SaveChangesAsync();
            await LogAsync(await ProjetIdFromFdtAsync(vm.FdtId), "Update", "FAT", f.Id, $"FAT modifié: {f.Nom}");
            TempData["Success"] = "FAT modifié.";
            return RedirectToAction(nameof(Index), new { fdtId = vm.FdtId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var f = await Db.Fats.FindAsync(id);
            if (f == null) return NotFound();
            var pId = await ProjetIdFromFdtAsync(f.FdtId);
            if (!await CanWriteAsync(pId)) return Forbid();
            var fId = f.FdtId;
            Db.Fats.Remove(f); await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "FAT", id, $"FAT supprimé: {f.Nom}");
            TempData["Success"] = "FAT supprimé.";
            return RedirectToAction(nameof(Index), new { fdtId = fId });
        }
    }
}
