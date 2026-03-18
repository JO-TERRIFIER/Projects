// ============================================================
// SmartGPON — Controllers/ProjetsController.cs — FRESH START
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
    public class ProjetsController : RbacControllerBase
    {
        public ProjetsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index()
        {
            var ids = await AccessibleProjetIdsAsync();
            var projets = await Db.Projets.Where(p => ids.Contains(p.Id))
                .Include(p => p.Client)
                .OrderBy(p => p.Nom)
                .Select(p => new ProjetDisplayVM
                {
                    Id = p.Id, Nom = p.Nom, Statut = p.Statut,
                    ClientNom = p.Client.Nom, ClientId = p.ClientId,
                    ZoneCount = p.Zones.Count
                }).ToListAsync();
            return View(projets);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var d = DenyVisiteur(); if (d != null) return d;
            ViewBag.Clients    = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
            ViewBag.SessionGuid = Guid.NewGuid().ToString("N");  // M1: généré ici (pas dans la vue)
            ViewBag.MaxSizeMb  = Configuration.GetValue<int>("FileUpload:MaxSizeMb");
            return View(new ProjetCreateVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjetCreateVM vm, string? sessionGuid)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!ModelState.IsValid)
            {
                // Rollback temp files
                if (!string.IsNullOrWhiteSpace(sessionGuid))
                {
                    var tempDir = TempDir(sessionGuid);
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
                }
                ViewBag.Clients     = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
                ViewBag.SessionGuid = Guid.NewGuid().ToString("N");
                ViewBag.MaxSizeMb   = Configuration.GetValue<int>("FileUpload:MaxSizeMb");
                TempData["Warn"]    = "Les fichiers ont été retirés suite à l'erreur. Veuillez les re-sélectionner.";
                return View(vm);
            }
            var entity = new SmartGPON.Core.Entities.Projet
            {
                ClientId = vm.ClientId, Nom = vm.Nom, Statut = vm.Statut
            };
            Db.Projets.Add(entity); await Db.SaveChangesAsync();
            await LogAsync(entity.Id, "Create", "Projet", entity.Id, $"Projet créé: {entity.Nom}");

            // Finaliser fichiers temp (D1 Option B)
            if (!string.IsNullOrWhiteSpace(sessionGuid)) await FinalizeTemp(sessionGuid, entity.Id, null);

            TempData["Success"] = "Projet créé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var p = await Db.Projets.FindAsync(id);
            if (p == null) return NotFound();
            if (!await CanWriteAsync(id)) return Forbid();
            ViewBag.Clients = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
            return View(new ProjetUpdateVM { Id = p.Id, ClientId = p.ClientId, Nom = p.Nom, Statut = p.Statut });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjetUpdateVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(vm.Id)) return Forbid();
            if (!ModelState.IsValid)
            {
                ViewBag.Clients = await Db.Clients.Where(c => c.IsActive).OrderBy(c => c.Nom).ToListAsync();
                return View(vm);
            }
            var p = await Db.Projets.FindAsync(vm.Id);
            if (p == null) return NotFound();
            p.ClientId = vm.ClientId; p.Nom = vm.Nom; p.Statut = vm.Statut;
            await Db.SaveChangesAsync();
            await LogAsync(p.Id, "Update", "Projet", p.Id, $"Projet modifié: {p.Nom}");
            TempData["Success"] = "Projet modifié.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, bool confirmed = false)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanWriteAsync(id)) return Forbid();
            var p = await Db.Projets.FindAsync(id);
            if (p == null) return NotFound();

            // A3 guard + A2 transaction
            var resources = await Db.Resources.Where(r => r.ProjetId == id).ToListAsync();
            if (resources.Count > 0 && !confirmed)
                return Json(new { needsConfirmation = true, count = resources.Count });

            using var tx = await Db.Database.BeginTransactionAsync();
            try
            {
                Db.Resources.RemoveRange(resources);
                Db.Projets.Remove(p);
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
                await LogAsync(id, resources.Count > 0 ? "DeleteWithFiles" : "Delete",
                    "Projet", id, $"Projet supprimé: {p.Nom} · {resources.Count} fichier(s)");
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Erreur lors de la suppression.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Success"] = "Projet supprimé.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ids = await AccessibleProjetIdsAsync();
            var p   = await Db.Projets.Include(p => p.Client).FirstOrDefaultAsync(p => p.Id == id);
            if (p == null || !ids.Contains(p.Id)) return NotFound();

            var resources = await Db.Resources
                .Where(r => r.ProjetId == id)
                .OrderBy(r => r.NomFichier)
                .ToListAsync();

            // Droits sur ce projet
            bool canUpload = await CanUploadHelper(id);
            bool canReview = await CanReviewHelper(id);

            // Mapping ResourceFileVM avec HasPendingDeletion
            var pendingResourceIds = await Db.DeletionRequests
                .Where(dr => dr.ProjetId == id && dr.Statut == SmartGPON.Core.Enums.DeletionStatut.EnAttente)
                .Select(dr => dr.ResourceId)
                .ToListAsync();

            bool canDel = await CanDeleteDirectHelper(id);
            bool canReq = await CanRequestDeleteHelper(id);

            var fileVms = resources.Select(r => new ResourceFileVM
            {
                Id = r.Id, ProjetId = r.ProjetId, ZoneId = r.ZoneId,
                NomFichier = r.NomFichier, FileExtension = r.FileExtension,
                ContentType = r.ContentType, FileSize = r.FileSize,
                UploadedAt = r.UploadedAt,
                CanDeleteDirect  = canDel,
                CanRequestDelete = canReq,
                HasPendingDeletion = pendingResourceIds.Contains(r.Id)
            }).ToList();

            // Chargement demandes en attente si reviewer
            var pendingVms = new List<DeletionRequestVM>();
            if (canReview)
            {
                var pending = await Db.DeletionRequests
                    .Include(dr => dr.Resource)
                    .Where(dr => dr.ProjetId == id && dr.Statut == SmartGPON.Core.Enums.DeletionStatut.EnAttente)
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

            var vm = new ProjetDetailsVM
            {
                Id = p.Id, Nom = p.Nom, ClientNom = p.Client.Nom, Statut = p.Statut,
                Files = fileVms, PendingDeletions = pendingVms,
                CanUpload = canUpload, CanReview = canReview
            };
            return View(vm);
        }

        // Helpers RBAC locaux (délèguent à la DB Identity + UPA)
        private async Task<bool> CanUploadHelper(int projetId)
        {
            if (IsSuperviseur) return true;
            return await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId && a.ProjetId == projetId && a.IsActive &&
                (a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.ChefProjet ||
                 a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.TechDessin));
        }
        private async Task<bool> CanDeleteDirectHelper(int projetId)
        {
            if (IsSuperviseur) return true;
            return await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId && a.ProjetId == projetId && a.IsActive &&
                a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.ChefProjet);
        }
        private async Task<bool> CanRequestDeleteHelper(int projetId)
        {
            return await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId && a.ProjetId == projetId && a.IsActive &&
                a.AssignmentType == SmartGPON.Core.Enums.AssignmentType.TechDessin);
        }
        private async Task<bool> CanReviewHelper(int projetId)
        {
            if (IsSuperviseur) return true;
            return await CanDeleteDirectHelper(projetId);
        }
    }
}
