using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ClientsController : RbacControllerBase
    {
        public ClientsController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        public async Task<IActionResult> Index()
        {
            var query = Db.Clients.AsNoTracking().AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                query = query.Where(c => Db.Projets.Any(p => p.ClientId == c.Id && p.ProjectManagerId == userId));
            }
            return View(await query.OrderBy(c => c.Nom).ToListAsync());
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public IActionResult Create() => View(new Client());

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create(Client m, string createDescription)
        {
            if (string.IsNullOrWhiteSpace(createDescription)) createDescription = "Création client";
            if (!ModelState.IsValid) return View(m);
            Db.Clients.Add(m);
            await Db.SaveChangesAsync();
            await LogAsync(null, "Create", nameof(Client), m.Id, createDescription);
            TempData["Success"] = "Client créé.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await Db.Clients.FindAsync(id);
            return m == null ? NotFound() : View(m);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(Client m, string editDescription)
        {
            if (string.IsNullOrWhiteSpace(editDescription)) editDescription = "Modification client";
            if (!ModelState.IsValid) return View(m);
            Db.Clients.Update(m);
            await Db.SaveChangesAsync();
            await LogAsync(null, "Edit", nameof(Client), m.Id, editDescription);
            TempData["Success"] = "Client mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur")]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Raison obligatoire."; return RedirectToAction(nameof(Index)); }
            var m = await Db.Clients.FindAsync(id);
            if (m != null)
            {
                Db.Clients.Remove(m);
                await Db.SaveChangesAsync();
                await LogAsync(null, "Delete", nameof(Client), id, reason);
            }
            TempData["Success"] = "Client supprimé.";
            return RedirectToAction(nameof(Index));
        }
    }
}

