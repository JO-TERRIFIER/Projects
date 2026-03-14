// ============================================================
// SmartGPON — Controllers/BpisController.cs — FRESH START
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
    public class BpisController : RbacControllerBase
    {
        public BpisController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Details(int id)
        {
            var bpi = await Db.Bpis
                .Include(b => b.Fdt).ThenInclude(f => f.Olt).ThenInclude(o => o.Zone)
                .Include(b => b.BoitiersEtage)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (bpi == null) return NotFound();
            return View(bpi);
        }

        public async Task<IActionResult> Index(int? fdtId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Bpis.Include(b => b.Fdt).ThenInclude(f => f.Olt).ThenInclude(o => o.Zone)
                .Where(b => ids.Contains(b.Fdt.Olt.Zone.ProjetId));
            if (fdtId.HasValue) q = q.Where(b => b.FdtId == fdtId.Value);
            var list = await q.OrderBy(b => b.Nom).Select(b => new BpiDisplayVM
            {
                Id = b.Id, Nom = b.Nom, Capacite = b.Capacite, Latitude = b.Latitude, Longitude = b.Longitude,
                FdtNom = b.Fdt.Nom, FdtId = b.FdtId
            }).ToListAsync();
            ViewBag.FdtId = fdtId;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create(int? fdtId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            return View(new BpiCreateVM { FdtId = fdtId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BpiCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var pId = await ProjetIdFromFdtAsync(vm.FdtId);
            if (!await CanWriteAsync(pId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var e = new SmartGPON.Core.Entities.Bpi
            { FdtId = vm.FdtId, Nom = vm.Nom, Capacite = vm.Capacite, Latitude = vm.Latitude, Longitude = vm.Longitude };
            Db.Bpis.Add(e); await Db.SaveChangesAsync();
            await LogAsync(pId, "Create", "BPI", e.Id, $"BPI créé: {e.Nom}");
            TempData["Success"] = "BPI créé.";
            return RedirectToAction(nameof(Index), new { fdtId = vm.FdtId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var b = await Db.Bpis.FindAsync(id);
            if (b == null) return NotFound();
            if (!await CanWriteAsync(await ProjetIdFromFdtAsync(b.FdtId))) return Forbid();
            return View(new BpiUpdateVM { Id = b.Id, FdtId = b.FdtId, Nom = b.Nom, Capacite = b.Capacite, Latitude = b.Latitude, Longitude = b.Longitude });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BpiUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(await ProjetIdFromFdtAsync(vm.FdtId))) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var b = await Db.Bpis.FindAsync(vm.Id);
            if (b == null) return NotFound();
            b.Nom = vm.Nom; b.Capacite = vm.Capacite; b.Latitude = vm.Latitude; b.Longitude = vm.Longitude;
            await Db.SaveChangesAsync();
            await LogAsync(await ProjetIdFromFdtAsync(vm.FdtId), "Update", "BPI", b.Id, $"BPI modifié: {b.Nom}");
            TempData["Success"] = "BPI modifié.";
            return RedirectToAction(nameof(Index), new { fdtId = vm.FdtId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var b = await Db.Bpis.FindAsync(id);
            if (b == null) return NotFound();
            var pId = await ProjetIdFromFdtAsync(b.FdtId);
            if (!await CanWriteAsync(pId)) return Forbid();
            var fId = b.FdtId;
            Db.Bpis.Remove(b); await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "BPI", id, $"BPI supprimé: {b.Nom}");
            TempData["Success"] = "BPI supprimé.";
            return RedirectToAction(nameof(Index), new { fdtId = fId });
        }
    }
}
