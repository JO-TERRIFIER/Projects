// ============================================================
// SmartGPON — Controllers/FdtsController.cs — FRESH START
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
    public class FdtsController : RbacControllerBase
    {
        public FdtsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? oltId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Fdts.Include(f => f.Olt).ThenInclude(o => o.Zone)
                .Where(f => ids.Contains(f.Olt.Zone.ProjetId));
            if (oltId.HasValue) q = q.Where(f => f.OltId == oltId.Value);
            var list = await q.OrderBy(f => f.Nom).Select(f => new FdtDisplayVM
            {
                Id = f.Id, Nom = f.Nom, Latitude = f.Latitude, Longitude = f.Longitude,
                OltNom = f.Olt.Nom, OltId = f.OltId,
                SplitterCount = f.Splitters.Count, FatCount = f.Fats.Count, BpiCount = f.Bpis.Count
            }).ToListAsync();
            ViewBag.OltId = oltId;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? oltId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var ids = await AccessibleProjetIdsAsync();
            ViewBag.Olts = await Db.Olts.Include(o => o.Zone).Where(o => ids.Contains(o.Zone.ProjetId)).OrderBy(o => o.Nom).ToListAsync();
            return View(new FdtCreateVM { OltId = oltId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FdtCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var pId = await ProjetIdFromOltAsync(vm.OltId);
            if (!await CanWriteAsync(pId)) return Forbid();
            if (!ModelState.IsValid)
            {
                var ids = await AccessibleProjetIdsAsync();
                ViewBag.Olts = await Db.Olts.Include(o => o.Zone).Where(o => ids.Contains(o.Zone.ProjetId)).OrderBy(o => o.Nom).ToListAsync();
                return View(vm);
            }
            var e = new SmartGPON.Core.Entities.Fdt
            { OltId = vm.OltId, Nom = vm.Nom, Latitude = vm.Latitude, Longitude = vm.Longitude };
            Db.Fdts.Add(e); await Db.SaveChangesAsync();
            await LogAsync(pId, "Create", "FDT", e.Id, $"FDT créé: {e.Nom}");
            TempData["Success"] = "FDT créé.";
            return RedirectToAction(nameof(Index), new { oltId = vm.OltId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var f = await Db.Fdts.FindAsync(id);
            if (f == null) return NotFound();
            if (!await CanWriteAsync(await ProjetIdFromOltAsync(f.OltId))) return Forbid();
            return View(new FdtUpdateVM { Id = f.Id, OltId = f.OltId, Nom = f.Nom, Latitude = f.Latitude, Longitude = f.Longitude });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FdtUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(await ProjetIdFromOltAsync(vm.OltId))) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var f = await Db.Fdts.FindAsync(vm.Id);
            if (f == null) return NotFound();
            f.Nom = vm.Nom; f.Latitude = vm.Latitude; f.Longitude = vm.Longitude;
            await Db.SaveChangesAsync();
            await LogAsync(await ProjetIdFromOltAsync(vm.OltId), "Update", "FDT", f.Id, $"FDT modifié: {f.Nom}");
            TempData["Success"] = "FDT modifié.";
            return RedirectToAction(nameof(Index), new { oltId = vm.OltId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var f = await Db.Fdts.FindAsync(id);
            if (f == null) return NotFound();
            var pId = await ProjetIdFromOltAsync(f.OltId);
            if (!await CanWriteAsync(pId)) return Forbid();
            var oId = f.OltId;
            Db.Fdts.Remove(f); await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "FDT", id, $"FDT supprimé: {f.Nom}");
            TempData["Success"] = "FDT supprimé.";
            return RedirectToAction(nameof(Index), new { oltId = oId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var f = await Db.Fdts.Include(f => f.Olt).Include(f => f.Splitters).Include(f => f.Fats).Include(f => f.Bpis)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (f == null) return NotFound();
            return View(f);
        }
    }
}
