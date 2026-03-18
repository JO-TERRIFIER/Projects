// ============================================================
// SmartGPON — Controllers/ZonesController.cs — FRESH START
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
    public class ZonesController : RbacControllerBase
    {
        public ZonesController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? projetId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Zones.Where(z => ids.Contains(z.ProjetId)).Include(z => z.Projet).AsQueryable();
            if (projetId.HasValue) q = q.Where(z => z.ProjetId == projetId.Value);
            var zones = await q.OrderBy(z => z.Nom).Select(z => new ZoneDisplayVM
            {
                Id = z.Id, Nom = z.Nom, Latitude = z.Latitude, Longitude = z.Longitude,
                ProjetNom = z.Projet.Nom, ProjetId = z.ProjetId,
                OltCount = z.Olts.Count
            }).ToListAsync();
            ViewBag.ProjetId = projetId;
            return View(zones);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projetId)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var ids = await AccessibleProjetIdsAsync();
            ViewBag.Projets     = await Db.Projets.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nom).ToListAsync();
            ViewBag.SessionGuid = Guid.NewGuid().ToString("N");  // M1: généré controller GET
            ViewBag.MaxSizeMb   = Configuration.GetValue<int>("FileUpload:MaxSizeMb");
            return View(new ZoneCreateVM { ProjetId = projetId ?? 0 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ZoneCreateVM vm, string? sessionGuid)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.ProjetId)) return Forbid();
            if (!ModelState.IsValid)
            {
                // Rollback temp files (A1: même pattern que Projets)
                if (!string.IsNullOrWhiteSpace(sessionGuid))
                {
                    var tempDir = TempDir(sessionGuid);
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
                }
                var ids = await AccessibleProjetIdsAsync();
                ViewBag.Projets     = await Db.Projets.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Nom).ToListAsync();
                ViewBag.SessionGuid = Guid.NewGuid().ToString("N");
                ViewBag.MaxSizeMb   = Configuration.GetValue<int>("FileUpload:MaxSizeMb");
                TempData["Warn"]    = "Les fichiers ont été retirés suite à l'erreur. Veuillez les re-sélectionner.";
                return View(vm);
            }
            var entity = new SmartGPON.Core.Entities.Zone
            {
                ProjetId = vm.ProjetId, Nom = vm.Nom, Latitude = vm.Latitude, Longitude = vm.Longitude
            };
            Db.Zones.Add(entity); await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Create", "Zone", entity.Id, $"Zone créée: {entity.Nom}");

            // Finaliser fichiers temp (A1: zoneId maintenant connu)
            if (!string.IsNullOrWhiteSpace(sessionGuid)) await FinalizeTemp(sessionGuid, vm.ProjetId, entity.Id);

            TempData["Success"] = "Zone créée.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var z = await Db.Zones.FindAsync(id);
            if (z == null) return NotFound();
            if (!await CanWriteAsync(z.ProjetId)) return Forbid();
            return View(new ZoneUpdateVM { Id = z.Id, ProjetId = z.ProjetId, Nom = z.Nom, Latitude = z.Latitude, Longitude = z.Longitude });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ZoneUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.ProjetId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);
            var z = await Db.Zones.FindAsync(vm.Id);
            if (z == null) return NotFound();
            z.Nom = vm.Nom; z.Latitude = vm.Latitude; z.Longitude = vm.Longitude;
            await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Update", "Zone", z.Id, $"Zone modifiée: {z.Nom}");
            TempData["Success"] = "Zone modifiée.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, bool confirmed = false)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var z = await Db.Zones.FindAsync(id);
            if (z == null) return NotFound();
            if (!await CanWriteAsync(z.ProjetId)) return Forbid();
            var pId = z.ProjetId;

            // A3 guard + A2 transaction
            var resources = await Db.Resources.Where(r => r.ProjetId == pId && r.ZoneId == id).ToListAsync();
            if (resources.Count > 0 && !confirmed)
                return Json(new { needsConfirmation = true, count = resources.Count });

            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                Db.Resources.RemoveRange(resources);
                Db.Zones.Remove(z);
                await Db.SaveChangesAsync();
                // Fichiers physiques APRÈS SaveChanges
                var uploadPath = Configuration.GetValue<string>("FileUpload:UploadPath") ?? "uploads";
                var uploadRoot = Path.Combine(Env.ContentRootPath, uploadPath);
                foreach (var r in resources)
                {
                    var fp = Path.Combine(uploadRoot, r.CheminFichier);
                    if (System.IO.File.Exists(fp)) System.IO.File.Delete(fp);
                }
                await tx.CommitAsync();
                await LogAsync(pId, resources.Count > 0 ? "DeleteWithFiles" : "Delete",
                    "Zone", id, $"Zone supprimée: {z.Nom} · {resources.Count} fichier(s)");
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Erreur lors de la suppression.";
                return RedirectToAction(nameof(Index), new { projetId = pId });
            }
            TempData["Success"] = "Zone supprimée.";
            return RedirectToAction(nameof(Index), new { projetId = pId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var z = await Db.Zones.Include(z => z.Projet).FirstOrDefaultAsync(z => z.Id == id);
            if (z == null) return NotFound();
            var ids = await AccessibleProjetIdsAsync();
            if (!ids.Contains(z.ProjetId)) return NotFound();

            var resources = await Db.Resources
                .Where(r => r.ProjetId == z.ProjetId && r.ZoneId == id)
                .OrderBy(r => r.NomFichier)
                .ToListAsync();

            // Droits
            bool canUpload = IsSuperviseur || await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId && a.ProjetId == z.ProjetId && a.IsActive &&
                (a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.ChefProjet ||
                 a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.TechDessin));

            bool canDelDirect = IsSuperviseur || await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId && a.ProjetId == z.ProjetId && a.IsActive &&
                a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.ChefProjet);

            bool canReqDel = await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId && a.ProjetId == z.ProjetId && a.IsActive &&
                a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.TechDessin);

            bool canReview = IsSuperviseur || canDelDirect;

            var pendingResourceIds = await Db.DeletionRequests
                .Where(dr => dr.ProjetId == z.ProjetId && dr.Statut == SmartGPON.Core.Enums.DeletionStatut.EnAttente)
                .Select(dr => dr.ResourceId)
                .ToListAsync();

            var fileVms = resources.Select(r => new ResourceFileVM
            {
                Id = r.Id, ProjetId = r.ProjetId, ZoneId = r.ZoneId,
                NomFichier = r.NomFichier, FileExtension = r.FileExtension,
                ContentType = r.ContentType, FileSize = r.FileSize,
                UploadedAt = r.UploadedAt,
                CanDeleteDirect   = canDelDirect,
                CanRequestDelete  = canReqDel,
                HasPendingDeletion = pendingResourceIds.Contains(r.Id)
            }).ToList();

            var pendingVms = new List<DeletionRequestVM>();
            if (canReview)
            {
                var pending = await Db.DeletionRequests
                    .Include(dr => dr.Resource)
                    .Where(dr => dr.ProjetId == z.ProjetId && dr.Statut == SmartGPON.Core.Enums.DeletionStatut.EnAttente)
                    .OrderBy(dr => dr.RequestedAt)
                    .ToListAsync();

                foreach (var req in pending)
                {
                    var requester = await Db.Users.FindAsync(req.RequestedByUserId);
                    pendingVms.Add(new DeletionRequestVM
                    {
                        Id = req.Id, ResourceId = req.ResourceId, ProjetId = req.ProjetId,
                        NomFichier = req.Resource.NomFichier,
                        RequestedByNom = requester != null ? $"{requester.FirstName} {requester.LastName}" : req.RequestedByUserId,
                        RequestedAt = req.RequestedAt
                    });
                }
            }

            return View(new ZoneDetailsVM
            {
                Id = z.Id, Nom = z.Nom, ProjetNom = z.Projet.Nom, ProjetId = z.ProjetId,
                Latitude = z.Latitude, Longitude = z.Longitude,
                Files = fileVms, PendingDeletions = pendingVms,
                CanUpload = canUpload, CanReview = canReview
            });
        }
    }
}
