// ============================================================
// SmartGPON — Controllers/OltsController.cs — FRESH START
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
    public class OltsController : RbacControllerBase
    {
        public OltsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? zoneId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Olts.Include(o => o.Zone).Where(o => ids.Contains(o.Zone.ProjetId));
            if (zoneId.HasValue) q = q.Where(o => o.ZoneId == zoneId.Value);
            var list = await q.OrderBy(o => o.Nom).Select(o => new OltDisplayVM
            {
                Id = o.Id, Nom = o.Nom, IpAddress = o.IpAddress, NbrePorts = o.NbrePorts,
                ZoneNom = o.Zone.Nom, ZoneId = o.ZoneId,
                FdtCount = o.Fdts.Count
            }).ToListAsync();
            ViewBag.ZoneId = zoneId;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? zoneId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var ids = await AccessibleProjetIdsAsync();
            ViewBag.Zones = await Db.Zones.Where(z => ids.Contains(z.ProjetId)).OrderBy(z => z.Nom).ToListAsync();
            return View(new OltCreateVM { ZoneId = zoneId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OltCreateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var pId = await ProjetIdFromZoneAsync(vm.ZoneId);
            if (!await CanWriteAsync(pId)) return Forbid();
            if (!ModelState.IsValid)
            {
                var ids = await AccessibleProjetIdsAsync();
                ViewBag.Zones = await Db.Zones.Where(z => ids.Contains(z.ProjetId)).OrderBy(z => z.Nom).ToListAsync();
                return View(vm);
            }
            var entity = new SmartGPON.Core.Entities.Olt
            { ZoneId = vm.ZoneId, Nom = vm.Nom, IpAddress = vm.IpAddress, NbrePorts = vm.NbrePorts };
            Db.Olts.Add(entity); await Db.SaveChangesAsync();
            await LogAsync(pId, "Create", "OLT", entity.Id, $"OLT créé: {entity.Nom}");
            TempData["Success"] = "OLT créé.";
            return RedirectToAction(nameof(Index), new { zoneId = vm.ZoneId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var o = await Db.Olts.FindAsync(id);
            if (o == null) return NotFound();
            var pId = await ProjetIdFromZoneAsync(o.ZoneId);
            if (!await CanWriteAsync(pId)) return Forbid();
            return View(new OltUpdateVM { Id = o.Id, ZoneId = o.ZoneId, Nom = o.Nom, IpAddress = o.IpAddress, NbrePorts = o.NbrePorts });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OltUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var pId = await ProjetIdFromZoneAsync(vm.ZoneId);
            if (!await CanWriteAsync(pId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var o = await Db.Olts.FindAsync(vm.Id);
            if (o == null) return NotFound();
            o.Nom = vm.Nom; o.IpAddress = vm.IpAddress; o.NbrePorts = vm.NbrePorts;
            await Db.SaveChangesAsync();
            await LogAsync(pId, "Update", "OLT", o.Id, $"OLT modifié: {o.Nom}");
            TempData["Success"] = "OLT modifié.";
            return RedirectToAction(nameof(Index), new { zoneId = vm.ZoneId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var o = await Db.Olts.FindAsync(id);
            if (o == null) return NotFound();
            var pId = await ProjetIdFromZoneAsync(o.ZoneId);
            if (!await CanWriteAsync(pId)) return Forbid();
            var zId = o.ZoneId;
            Db.Olts.Remove(o); await Db.SaveChangesAsync();
            await LogAsync(pId, "Delete", "OLT", id, $"OLT supprimé: {o.Nom}");
            TempData["Success"] = "OLT supprimé.";
            return RedirectToAction(nameof(Index), new { zoneId = zId });
        }
    }
}
