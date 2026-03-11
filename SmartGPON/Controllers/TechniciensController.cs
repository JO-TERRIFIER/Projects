using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class TechniciensController : RbacControllerBase
    {
        public TechniciensController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        public async Task<IActionResult> Index()
        {
            var query = Db.Techniciens.AsNoTracking().Include(t => t.Projet).Include(t => t.Zone).AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                var projIds = Db.Projets.Where(p => p.ProjectManagerId == userId).Select(p => p.Id).ToList();
                query = query.Where(t => t.ProjetId == 0 || projIds.Contains(t.ProjetId));
            }
            return View(await query.OrderBy(t => t.Nom).ToListAsync());
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom");
            ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom");
            return View(new Technicien());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create(Technicien m, string createDescription)
        {
            if (string.IsNullOrWhiteSpace(createDescription)) createDescription = "Création technicien";
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
                ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom", m.ZoneId);
                return View(m);
            }

            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            Db.Techniciens.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(m.ProjetId, "Create", nameof(Technicien), m.Id, createDescription);
            TempData["Success"] = "Technicien créé.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await Db.Techniciens.FindAsync(id);
            if (m == null) return NotFound();
            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
            ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom", m.ZoneId);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(Technicien m, string editDescription)
        {
            if (string.IsNullOrWhiteSpace(editDescription)) editDescription = "Modification technicien";
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync(), "Id", "Nom", m.ProjetId);
                ViewBag.Zones = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await Db.Zones.AsNoTracking().OrderBy(z => z.Nom).ToListAsync(), "Id", "Nom", m.ZoneId);
                return View(m);
            }

            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            Db.Techniciens.Update(m);
            await Db.SaveChangesAsync();
            await LogAsync(m.ProjetId, "Edit", nameof(Technicien), m.Id, editDescription);
            TempData["Success"] = "Technicien mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            var m = await Db.Techniciens.FindAsync(id);
            if (m == null) return NotFound();
            if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Raison obligatoire."; return RedirectToAction(nameof(Index)); }

            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            Db.Techniciens.Remove(m);
            await Db.SaveChangesAsync();
            await LogAsync(m.ProjetId, "Delete", nameof(Technicien), m.Id, reason);
            TempData["Success"] = "Technicien supprimé.";
            return RedirectToAction(nameof(Index));
        }
    }
}

