using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public abstract class ScopedEquipmentControllerBase<T> : RbacControllerBase where T : class, new()
    {
        protected ScopedEquipmentControllerBase(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        protected abstract IQueryable<T> QueryForIndex();
        protected abstract Task<T?> FindAsync(int id);
        protected abstract int GetEntityId(T entity);
        protected abstract Task<int> ResolveProjetIdAsync(T entity);
        protected abstract Task<int> ResolveProjetIdFromIdAsync(int id);
        protected abstract string EntityName { get; }
        protected abstract Task PopulateCreateEditBagsAsync(T? entity = null);

        public async Task<IActionResult> Index() => View(await QueryForIndex().AsNoTracking().OrderBy(e => EF.Property<string>(e, "Nom")).ToListAsync());

        [Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> Create()
        {
            await PopulateCreateEditBagsAsync();
            return View(new T());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> Create(T entity, string createDescription)
        {
            if (string.IsNullOrWhiteSpace(createDescription)) createDescription = "Création " + EntityName;
            if (!ModelState.IsValid)
            {
                await PopulateCreateEditBagsAsync(entity);
                return View(entity);
            }

            var projetId = await ResolveProjetIdAsync(entity);
            if (projetId == 0) return BadRequest("Parent invalide.");

            var allowed = IsSuperviseur || await CanChefProjectAsync(projetId) || await CanTechTerrainProjectAsync(projetId);
            if (!allowed) return Forbid();

            Db.Set<T>().Add(entity);
            await Db.SaveChangesAsync();
            await LogAsync(projetId, "Create", EntityName, GetEntityId(entity), createDescription);
            TempData["Success"] = $"{EntityName} créé.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await FindAsync(id);
            if (entity == null) return NotFound();

            var projetId = await ResolveProjetIdFromIdAsync(id);
            if (projetId == 0) return NotFound();

            var canSuperviseurChef = IsSuperviseur || await CanChefProjectAsync(projetId);
            var canTech = await CanTechTerrainProjectAsync(projetId);
            if (!canSuperviseurChef && !canTech) return Forbid();

            await PopulateCreateEditBagsAsync(entity);
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> Edit(T entity, string editDescription)
        {
            if (string.IsNullOrWhiteSpace(editDescription)) editDescription = "Modification " + EntityName;
            if (!ModelState.IsValid)
            {
                await PopulateCreateEditBagsAsync(entity);
                return View(entity);
            }

            var projetId = await ResolveProjetIdAsync(entity);
            if (projetId == 0) return BadRequest("Parent invalide.");

            if (IsSuperviseur || await CanChefProjectAsync(projetId))
            {
                Db.Set<T>().Update(entity);
                await Db.SaveChangesAsync();
                await LogAsync(projetId, "Edit", EntityName, GetEntityId(entity), editDescription);
                TempData["Success"] = $"{EntityName} mis à jour.";
                return RedirectToAction(nameof(Index));
            }

            if (await CanTechTerrainProjectAsync(projetId))
            {
                await Approvals.CreateAsync(projetId, CurrentUserId, EntityName, GetEntityId(entity), ApprovalActionType.EditEquipment, editDescription);
                await LogAsync(projetId, "RequestEdit", EntityName, GetEntityId(entity), editDescription);
                TempData["Success"] = "Demande de modification envoyée au ChefProjet.";
                return RedirectToAction(nameof(Index));
            }

            return Forbid();
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet,TechTerrain")]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Raison obligatoire."; return RedirectToAction(nameof(Index)); }

            var entity = await FindAsync(id);
            if (entity == null) return NotFound();
            var projetId = await ResolveProjetIdFromIdAsync(id);

            if (IsSuperviseur || await CanChefProjectAsync(projetId))
            {
                Db.Set<T>().Remove(entity);
                await Db.SaveChangesAsync();
                await LogAsync(projetId, "Delete", EntityName, id, reason);
                TempData["Success"] = $"{EntityName} supprimé.";
                return RedirectToAction(nameof(Index));
            }

            if (await CanTechTerrainProjectAsync(projetId))
            {
                await Approvals.CreateAsync(projetId, CurrentUserId, EntityName, id, ApprovalActionType.DeleteEquipment, reason);
                await LogAsync(projetId, "RequestDelete", EntityName, id, reason);
                TempData["Success"] = "Demande de suppression envoyée au ChefProjet.";
                return RedirectToAction(nameof(Index));
            }

            return Forbid();
        }
    }
}

