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
    public class ProjetsController : RbacControllerBase
    {
        public ProjetsController(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit) { }

        public async Task<IActionResult> Index()
        {
            var query = Db.Projets.AsNoTracking().Include(p => p.Client).Include(p => p.Resources).AsQueryable();
            if (IsChefProjet && !IsSuperviseur)
            {
                var userId = CurrentUserId;
                query = query.Where(p => p.ProjectManagerId == userId);
            }
            return View(await query.OrderBy(p => p.Nom).ToListAsync());
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Clients = await Db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            return View(new Projet());
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Create(Projet m, string createDescription, IFormFileCollection uploadedFiles, [FromServices] IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(createDescription)) createDescription = "Création projet";
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = await Db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
                return View(m);
            }

            Db.Projets.Add(m);
            await Db.SaveChangesAsync();
            
            // Gestion de l'upload conditionnel
            var isTechDessin = IsTechDessin && !IsSuperviseur && !IsChefProjet;
            var canUpload = IsSuperviseur || IsChefProjet || isTechDessin;
            if (uploadedFiles != null && uploadedFiles.Any() && canUpload)
            {
                var folder = Path.Combine(env.WebRootPath, "resources", "projets", m.Id.ToString());
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
                                ProjetId = m.Id,
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

            if (IsChefProjet && !IsSuperviseur)
            {
                m.ProjectManagerId = CurrentUserId;
                await Db.SaveChangesAsync();
            }

            await LogAsync(m.Id, "Create", nameof(Projet), m.Id, createDescription);
            TempData["Success"] = "Projet créé.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(int id)
        {
            var m = await Db.Projets.FindAsync(id);
            if (m == null) return NotFound();

            if (!IsSuperviseur && !await CanChefProjectAsync(m.Id)) return Forbid();

            ViewBag.Clients = await Db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
            ViewBag.Resources = await Db.Resources.AsNoTracking().Where(r => r.ProjetId == id).OrderByDescending(r => r.DateUpload).ToListAsync();
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Edit(Projet m, string editDescription)
        {
            if (string.IsNullOrWhiteSpace(editDescription)) editDescription = "Modification projet";
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = await Db.Clients.AsNoTracking().OrderBy(c => c.Nom).ToListAsync();
                return View(m);
            }

            if (!IsSuperviseur && !await CanChefProjectAsync(m.Id)) return Forbid();

            Db.Projets.Update(m);
            await Db.SaveChangesAsync();
            await LogAsync(m.Id, "Edit", nameof(Projet), m.Id, editDescription);
            TempData["Success"] = "Projet mis à jour.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet")]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Raison obligatoire."; return RedirectToAction(nameof(Index)); }

            var m = await Db.Projets.FindAsync(id);
            if (m == null) return NotFound();

            if (IsSuperviseur)
            {
                Db.Projets.Remove(m);
                await Db.SaveChangesAsync();
                await LogAsync(m.Id, "Delete", nameof(Projet), m.Id, reason);
                TempData["Success"] = "Projet supprimé.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanChefProjectAsync(m.Id)) return Forbid();

            await Approvals.CreateAsync(m.Id, CurrentUserId, nameof(Projet), m.Id, ApprovalActionType.DeleteProjet, reason);
            await LogAsync(m.Id, "RequestDelete", nameof(Projet), m.Id, reason);
            TempData["Success"] = "Demande de suppression envoyée aux Superviseurs.";
            return RedirectToAction(nameof(Index));
        }
    }
}

