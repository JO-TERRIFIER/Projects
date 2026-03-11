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
    public class ZonesController : RbacControllerBase
    {
        public ZonesController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        public async Task<IActionResult> Index()
        {
            var query = Db.Zones.AsNoTracking().Include(z => z.Projet).Include(z => z.Resources).AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                query = query.Where(z => z.Projet.ProjectManagerId == userId);
            }
            return View(await query.OrderBy(z => z.Nom).ToListAsync());
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create()
        {
            var pQuery = Db.Projets.AsNoTracking().AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                pQuery = pQuery.Where(p => p.ProjectManagerId == userId);
            }
            ViewBag.Projets = await pQuery.OrderBy(p => p.Nom).ToListAsync();
            return View(new Zone());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create(Zone m, string createDescription, IFormFileCollection uploadedFiles, [FromServices] IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(createDescription)) createDescription = "Création zone";
            if (!ModelState.IsValid)
            {
                var pQuery = Db.Projets.AsNoTracking().AsQueryable();
                if (IsChefProjet && !IsSuperviseur)
                {
                    var userId = CurrentUserId;
                    pQuery = pQuery.Where(p => p.ProjectManagerId == userId);
                }
                ViewBag.Projets = await pQuery.OrderBy(p => p.Nom).ToListAsync();
                return View(m);
            }

            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            Db.Zones.Add(m);
            await Db.SaveChangesAsync();
            
            // Gestion de l'upload conditionnel
            var isTechDessin = IsTechDessin && !IsSuperviseur && !IsChefProjet;
            var canUpload = IsSuperviseur || IsChefProjet || isTechDessin;
            if (uploadedFiles != null && uploadedFiles.Any() && canUpload)
            {
                var folder = Path.Combine(env.WebRootPath, "resources", "zones", m.Id.ToString());
                Directory.CreateDirectory(folder);
                var allowed = isTechDessin ? new[] { ".dwg" } : new[] { ".dwg", ".pdf", ".png", ".jpg", ".jpeg", ".xlsx" };

                foreach (var file in uploadedFiles)
                {
                    if (file.Length > 0)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (allowed.Contains(ext))
                        {
                            var fileName = $"{Guid.NewGuid():N}{ext}";
                            var filePath = Path.Combine(folder, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                            Db.Resources.Add(new Resource
                            {
                                ZoneId = m.Id,
                                NomFichier = file.FileName,
                                CheminFichier = filePath,
                                TypeFichier = ext,
                                TailleFichier = file.Length
                            });
                        }
                    }
                }
                await Db.SaveChangesAsync();
            }
            await LogAsync(m.ProjetId, "Create", nameof(Zone), m.Id, createDescription);
            TempData["Success"] = "Zone créée.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await Db.Zones.FindAsync(id);
            if (m == null) return NotFound();
            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            var pQuery = Db.Projets.AsNoTracking().AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                pQuery = pQuery.Where(p => p.ProjectManagerId == userId);
            }
            ViewBag.Projets = await pQuery.OrderBy(p => p.Nom).ToListAsync();
            ViewBag.Resources = await Db.Resources.AsNoTracking().Where(r => r.ZoneId == id).OrderByDescending(r => r.DateUpload).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(Zone m, string editDescription)
        {
            if (string.IsNullOrWhiteSpace(editDescription)) editDescription = "Modification zone";
            if (!ModelState.IsValid)
            {
                ViewBag.Projets = await Db.Projets.AsNoTracking().OrderBy(p => p.Nom).ToListAsync();
                return View(m);
            }

            if (!IsSuperviseur && !await CanChefProjectAsync(m.ProjetId)) return Forbid();

            Db.Zones.Update(m);
            await Db.SaveChangesAsync();
            await LogAsync(m.ProjetId, "Edit", nameof(Zone), m.Id, editDescription);
            TempData["Success"] = "Zone mise à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Raison obligatoire."; return RedirectToAction(nameof(Index)); }
            var m = await Db.Zones.FindAsync(id);
            if (m == null) return NotFound();

            if (IsSuperviseur)
            {
                Db.Zones.Remove(m);
                await Db.SaveChangesAsync();
                await LogAsync(m.ProjetId, "Delete", nameof(Zone), m.Id, reason);
                TempData["Success"] = "Zone supprimée.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanChefProjectAsync(m.ProjetId)) return Forbid();

            await Approvals.CreateAsync(m.ProjetId, CurrentUserId, nameof(Zone), m.Id, ApprovalActionType.DeleteZone, reason);
            await LogAsync(m.ProjetId, "RequestDelete", nameof(Zone), m.Id, reason);
            TempData["Success"] = "Demande de suppression envoyée aux Superviseurs.";
            return RedirectToAction(nameof(Index));
        }
    }
}

