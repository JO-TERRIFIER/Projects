// ============================================================
// SmartGPON — Controllers/ResourcesController.cs — File Upload
// Plan v4 · P3 · RBAC complet · MIME natif · Stockage hors wwwroot
// ============================================================
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Infrastructure.Helpers;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class ResourcesController : RbacControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ResourcesController(
            ApplicationDbContext db,
            IUserProjectAssignmentService a,
            IAuditLogService au,
            IWebHostEnvironment env,
            IConfiguration config)
            : base(db, a, au)
        {
            _env = env;
            _config = config;
        }

        // ── Index ────────────────────────────────────────────────
        public async Task<IActionResult> Index(int? projetId)
        {
            var ids = await AccessibleProjetIdsAsync();
            var q = Db.Resources
                .Include(r => r.Projet)
                .Include(r => r.Zone)
                .Where(r => ids.Contains(r.ProjetId));
            if (projetId.HasValue) q = q.Where(r => r.ProjetId == projetId.Value);
            var list = await q.OrderBy(r => r.NomFichier)
                .Select(r => new ResourceDisplayVM
                {
                    Id = r.Id,
                    NomFichier = r.NomFichier,
                    CheminFichier = r.CheminFichier,
                    ProjetId = r.ProjetId,
                    ZoneId = r.ZoneId,
                    ProjetNom = r.Projet.Nom,
                    ZoneNom = r.Zone != null ? r.Zone.Nom : null
                }).ToListAsync();
            return View(list);
        }

        // ── Helpers RBAC — A1: code EF complet ──────────────────
        private async Task<bool> CanUpload(int projetId)
        {
            if (IsSuperviseur) return true;
            return await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId &&
                a.ProjetId == projetId &&
                a.IsActive &&
                (a.AssignmentType == AssignmentType.ChefProjet ||
                 a.AssignmentType == AssignmentType.TechDessin));
        }

        private async Task<bool> CanDeleteDirect(int projetId)
        {
            if (IsSuperviseur) return true;
            return await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId &&
                a.ProjetId == projetId &&
                a.IsActive &&
                a.AssignmentType == AssignmentType.ChefProjet);
        }

        private async Task<bool> CanRequestDelete(int projetId)
        {
            return await Db.UserProjectAssignments.AnyAsync(a =>
                a.UserId == CurrentUserId &&
                a.ProjetId == projetId &&
                a.IsActive &&
                a.AssignmentType == AssignmentType.TechDessin);
        }

        private async Task<bool> CanReview(int projetId)
        {
            if (IsSuperviseur) return true;
            return await CanDeleteDirect(projetId);
        }

        // ── Upload GET ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Upload(int projetId, int? zoneId)
        {
            if (!await CanUpload(projetId)) return Forbid();
            var allowedExt = _config.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            ViewBag.AllowedExtensions = allowedExt;
            ViewBag.MaxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMb");
            return View(new ResourceUploadVM { ProjetId = projetId, ZoneId = zoneId });
        }

        // ── Upload POST ──────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ResourceUploadVM vm)
        {
            if (!await CanUpload(vm.ProjetId)) return Forbid();

            var allowedExt  = _config.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var maxSizeBytes = _config.GetValue<int>("FileUpload:MaxSizeMb") * 1024L * 1024L;
            var file = vm.File;

            // 1. Extension whitelist
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
            {
                ModelState.AddModelError("File", $"Extension '{ext}' non autorisée.");
                ViewBag.AllowedExtensions = allowedExt;
                ViewBag.MaxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMb");
                return View(vm);
            }

            // 2. Taille max
            if (file.Length > maxSizeBytes)
            {
                ModelState.AddModelError("File", $"Fichier trop volumineux (max {_config.GetValue<int>("FileUpload:MaxSizeMb")} MB).");
                ViewBag.AllowedExtensions = allowedExt;
                ViewBag.MaxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMb");
                return View(vm);
            }

            // 3. MIME magic bytes — A4: MimeHelper natif
            using var stream = file.OpenReadStream();
            var detectedMime = MimeHelper.DetectMime(stream);
            if (detectedMime == null || !MimeHelper.IsAllowed(ext, detectedMime))
            {
                ModelState.AddModelError("File", "Type de fichier non reconnu ou invalide. Les extensions renommées sont rejetées.");
                ViewBag.AllowedExtensions = allowedExt;
                ViewBag.MaxSizeMb = _config.GetValue<int>("FileUpload:MaxSizeMb");
                return View(vm);
            }

            // 4. Construire chemin physique hors wwwroot — A1
            var guid = Guid.NewGuid().ToString("N");
            string relativePath;
            if (vm.ZoneId.HasValue)
                relativePath = Path.Combine("projets", vm.ProjetId.ToString(), "zones", vm.ZoneId.Value.ToString(), guid + ext);
            else
                relativePath = Path.Combine("projets", vm.ProjetId.ToString(), guid + ext);

            var uploadRoot = Path.Combine(_env.ContentRootPath, _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads");
            var fullPath   = Path.Combine(uploadRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            stream.Position = 0;
            using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            // 5. Persister en DB
            var canonicalMime = MimeHelper.GetCanonicalMime(ext);
            var resource = new Resource
            {
                ProjetId         = vm.ProjetId,
                ZoneId           = vm.ZoneId,
                NomFichier       = Path.GetFileName(file.FileName),
                CheminFichier    = relativePath,
                UploadedByUserId = CurrentUserId,
                UploadedAt       = DateTime.UtcNow,
                FileSize         = file.Length,
                FileExtension    = ext,
                ContentType      = canonicalMime
            };
            Db.Resources.Add(resource);
            await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Upload", "Resource", resource.Id, $"Fichier uploadé: {resource.NomFichier}");
            TempData["Success"] = "Fichier uploadé avec succès.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }

        // ── Download GET — R2: NotFound anti-énumération ─────────
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var resource = await Db.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            var ids = await AccessibleProjetIdsAsync();
            if (!ids.Contains(resource.ProjetId)) return NotFound(); // jamais Forbid

            var uploadRoot = Path.Combine(_env.ContentRootPath, _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads");
            var fullPath   = Path.Combine(uploadRoot, resource.CheminFichier);
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            return PhysicalFile(fullPath, resource.ContentType, resource.NomFichier);
        }

        // ── Delete POST — A3: guard hasPending ──────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var resource = await Db.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            var projetId = resource.ProjetId;

            if (await CanDeleteDirect(projetId))
            {
                // A3: Guard demandes en attente
                var hasPending = await Db.DeletionRequests.AnyAsync(r =>
                    r.ResourceId == id && r.Statut == DeletionStatut.EnAttente);
                if (hasPending)
                {
                    TempData["Error"] = "Impossible de supprimer: des demandes de suppression sont en attente d'approbation.";
                    return RedirectToAction(nameof(Index), new { projetId });
                }

                // Supprimer physique
                var uploadRoot = Path.Combine(_env.ContentRootPath, _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads");
                var fullPath   = Path.Combine(uploadRoot, resource.CheminFichier);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

                Db.Resources.Remove(resource);
                await Db.SaveChangesAsync();
                await LogAsync(projetId, "Delete", "Resource", id, $"Fichier supprimé: {resource.NomFichier}");
                TempData["Success"] = "Fichier supprimé.";
            }
            else if (await CanRequestDelete(projetId))
            {
                // Créer DeletionRequest
                var req = new DeletionRequest
                {
                    ResourceId         = id,
                    RequestedByUserId  = CurrentUserId,
                    ProjetId           = projetId,
                    Statut             = DeletionStatut.EnAttente,
                    RequestedAt        = DateTime.UtcNow
                };
                Db.DeletionRequests.Add(req);
                await Db.SaveChangesAsync();
                await LogAsync(projetId, "RequestDelete", "Resource", id, $"Demande suppression: {resource.NomFichier}");
                TempData["Success"] = "Demande de suppression envoyée.";
            }
            else
            {
                return Forbid();
            }

            return RedirectToAction(nameof(Index), new { projetId });
        }

        // ── PendingDeletions GET ─────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PendingDeletions(int projetId)
        {
            if (!await CanReview(projetId)) return Forbid();

            var requests = await Db.DeletionRequests
                .Include(r => r.Resource)
                .Where(r => r.ProjetId == projetId && r.Statut == DeletionStatut.EnAttente)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            var vms = new List<DeletionRequestVM>();
            foreach (var req in requests)
            {
                var requester = await Db.Users.FindAsync(req.RequestedByUserId);
                vms.Add(new DeletionRequestVM
                {
                    Id               = req.Id,
                    ResourceId       = req.ResourceId,
                    ProjetId         = req.ProjetId,
                    NomFichier       = req.Resource.NomFichier,
                    RequestedByNom   = requester != null ? $"{requester.FirstName} {requester.LastName}" : req.RequestedByUserId,
                    RequestedAt      = req.RequestedAt
                });
            }
            return View(vms);
        }

        // ── ApproveDelete POST — A3: guard hasPending ───────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDelete(int requestId)
        {
            var req = await Db.DeletionRequests.Include(r => r.Resource).FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return NotFound();
            if (!await CanReview(req.ProjetId)) return Forbid();

            var resource = req.Resource;

            // Supprimer physique
            var uploadRoot = Path.Combine(_env.ContentRootPath, _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads");
            var fullPath   = Path.Combine(uploadRoot, resource.CheminFichier);
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

            // Marquer approuvé
            req.Statut          = DeletionStatut.Approuve;
            req.ReviewedByUserId = CurrentUserId;
            req.ReviewedAt      = DateTime.UtcNow;
            await Db.SaveChangesAsync();

            // Supprimer Resource en DB
            Db.Resources.Remove(resource);
            await Db.SaveChangesAsync();

            await LogAsync(req.ProjetId, "ApproveDelete", "Resource", resource.Id, $"Suppression approuvée: {resource.NomFichier}");
            TempData["Success"] = "Fichier supprimé après approbation.";
            return RedirectToAction(nameof(PendingDeletions), new { projetId = req.ProjetId });
        }

        // ── RejectDelete POST ────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectDelete(int requestId, string? commentaire)
        {
            var req = await Db.DeletionRequests.Include(r => r.Resource).FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null) return NotFound();
            if (!await CanReview(req.ProjetId)) return Forbid();

            req.Statut           = DeletionStatut.Rejete;
            req.ReviewedByUserId = CurrentUserId;
            req.ReviewedAt       = DateTime.UtcNow;
            req.CommentaireRejet = commentaire;
            await Db.SaveChangesAsync();

            await LogAsync(req.ProjetId, "RejectDelete", "Resource", req.ResourceId, $"Suppression rejetée: {req.Resource.NomFichier}");
            TempData["Info"] = "Demande de suppression rejetée.";
            return RedirectToAction(nameof(PendingDeletions), new { projetId = req.ProjetId });
        }

        // ── Edit GET — A5: NomFichier uniquement ─────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            var r = await Db.Resources.FindAsync(id);
            if (r == null) return NotFound();
            var ids = await AccessibleProjetIdsAsync();
            if (!ids.Contains(r.ProjetId)) return NotFound();

            ViewBag.FileExtension    = r.FileExtension;
            ViewBag.FileSize         = r.FileSize;
            ViewBag.ContentType      = r.ContentType;
            ViewBag.UploadedAt       = r.UploadedAt;
            ViewBag.UploadedByUserId = r.UploadedByUserId;

            return View(new ResourceRenameVM { Id = r.Id, ProjetId = r.ProjetId, NomFichier = r.NomFichier });
        }

        // ── Edit POST — A5: NomFichier uniquement ────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ResourceRenameVM vm)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            if (!await CanUpload(vm.ProjetId)) return Forbid();
            if (!ModelState.IsValid) return View(vm);

            var r = await Db.Resources.FindAsync(vm.Id);
            if (r == null) return NotFound();

            var ancienNom = r.NomFichier;
            r.NomFichier = vm.NomFichier.Trim();
            await Db.SaveChangesAsync();
            await LogAsync(vm.ProjetId, "Rename", "Resource", r.Id, $"Renommage: '{ancienNom}' → '{r.NomFichier}'");
            TempData["Success"] = "Nom de fichier mis à jour.";
            return RedirectToAction(nameof(Index), new { projetId = vm.ProjetId });
        }
        // ── GetFiles (AJAX · popup Documents + Edit liste) ──────
        [HttpGet]
        public async Task<IActionResult> GetFiles(int projetId, int? zoneId)
        {
            var ids = await AccessibleProjetIdsAsync();
            if (!ids.Contains(projetId)) return NotFound();

            var q = Db.Resources.Where(r => r.ProjetId == projetId);
            if (zoneId.HasValue) q = q.Where(r => r.ZoneId == zoneId.Value);
            else q = q.Where(r => r.ZoneId == null);

            var list = await q.OrderBy(r => r.NomFichier).Select(r => new ResourceFileVM
            {
                Id              = r.Id,
                NomFichier      = r.NomFichier,
                FileExtension   = r.FileExtension ?? Path.GetExtension(r.NomFichier),
                FileSize        = r.FileSize,
                ContentType     = r.ContentType ?? "application/octet-stream",
                UploadedAt      = r.UploadedAt,
                HasPendingDeletion = Db.DeletionRequests.Any(dr =>
                    dr.ResourceId == r.Id && dr.Statut == DeletionStatut.EnAttente)
            }).ToListAsync();

            return Json(list);
        }

        // ── UploadAjax (Edit Projets/Zones) ──────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAjax(int projetId, int? zoneId, IFormFile file)
        {
            if (!await CanUpload(projetId))
                return Json(new { success = false, error = "Accès non autorisé." });

            var allowedExt   = _config.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var maxSizeBytes = _config.GetValue<int>("FileUpload:MaxSizeMb") * 1024L * 1024L;
            var ext          = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExt.Contains(ext))
                return Json(new { success = false, error = $"Extension '{ext}' non autorisée." });
            if (file.Length > maxSizeBytes)
                return Json(new { success = false, error = $"Fichier trop volumineux (max {_config.GetValue<int>("FileUpload:MaxSizeMb")} MB)." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var detectedMime = MimeHelper.DetectMime(ms);
            if (detectedMime == null || !MimeHelper.IsAllowed(ext, detectedMime))
                return Json(new { success = false, error = "Type MIME invalide. Extension renommée rejetée." });

            var guid         = Guid.NewGuid().ToString("N");
            var relativePath = zoneId.HasValue
                ? Path.Combine("projets", projetId.ToString(), "zones", zoneId.Value.ToString(), guid + ext)
                : Path.Combine("projets", projetId.ToString(), guid + ext);

            var uploadRoot = Path.Combine(_env.ContentRootPath, _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads");
            var fullPath   = Path.Combine(uploadRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            ms.Position = 0;
            using (var fs = new FileStream(fullPath, FileMode.Create)) await ms.CopyToAsync(fs);

            var resource = new Resource
            {
                ProjetId          = projetId,
                ZoneId            = zoneId,
                NomFichier        = Path.GetFileNameWithoutExtension(file.FileName),
                CheminFichier     = relativePath,
                UploadedByUserId  = CurrentUserId,
                UploadedAt        = DateTime.UtcNow,
                FileSize          = file.Length,
                FileExtension     = ext,
                ContentType       = detectedMime
            };
            Db.Resources.Add(resource);
            await Db.SaveChangesAsync();
            await LogAsync(projetId, "Upload", "Resource", resource.Id, $"Upload AJAX: {file.FileName}");

            return Json(new { success = true, id = resource.Id, nomFichier = resource.NomFichier,
                fileSize = resource.FileSize, fileExtension = resource.FileExtension });
        }

        // ── DeleteAjax (Edit Projets/Zones) ──────────────────────
        [HttpDelete, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var r = await Db.Resources.FindAsync(id);
            if (r == null) return NotFound();

            var canDirect  = await CanDeleteDirect(r.ProjetId);
            var canRequest = await CanRequestDelete(r.ProjetId);
            if (!canDirect && !canRequest)
                return Json(new { success = false, error = "Accès non autorisé." });

            var hasPending = await Db.DeletionRequests.AnyAsync(dr =>
                dr.ResourceId == id && dr.Statut == DeletionStatut.EnAttente);

            if (canDirect && !hasPending)
            {
                var uploadRoot = Path.Combine(_env.ContentRootPath, _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads");
                var fullPath   = Path.Combine(uploadRoot, r.CheminFichier);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                Db.Resources.Remove(r);
                await Db.SaveChangesAsync();
                await LogAsync(r.ProjetId, "DeleteAjax", "Resource", id, $"Suppression AJAX: {r.NomFichier}");
                return Json(new { success = true, requestedDeletion = false });
            }

            if (canRequest)
            {
                var req = new DeletionRequest
                {
                    ResourceId           = id,
                    ProjetId             = r.ProjetId,
                    RequestedByUserId    = CurrentUserId,
                    RequestedAt          = DateTime.UtcNow,
                    Statut               = DeletionStatut.EnAttente
                };
                Db.DeletionRequests.Add(req);
                await Db.SaveChangesAsync();
                await LogAsync(r.ProjetId, "RequestDeleteAjax", "Resource", id, $"Demande suppression AJAX: {r.NomFichier}");
                return Json(new { success = true, requestedDeletion = true });
            }

            return Json(new { success = false, error = "Suppression directe bloquée (demande en attente)." });
        }

        // ── UploadTemp (Create Projets + Zones) ──────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTemp(string sessionGuid, IFormFile file)
        {
            // RBAC A3: exclure Visiteur
            if (User.IsInRole(UserRoles.Visiteur))
                return Json(new { success = false, error = "Accès non autorisé." });

            // RBAC A3: au moins 1 projet accessible
            var ids = await AccessibleProjetIdsAsync();
            if (!ids.Any())
                return Json(new { success = false, error = "Aucun projet accessible." });

            // Validation sessionGuid (éviter path traversal)
            if (string.IsNullOrWhiteSpace(sessionGuid) || sessionGuid.Length > 64 ||
                !System.Text.RegularExpressions.Regex.IsMatch(sessionGuid, @"^[a-zA-Z0-9\-]+$"))
                return Json(new { success = false, error = "Session invalide." });

            var tempDir = Path.Combine(_env.ContentRootPath,
                _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads", "temp", sessionGuid);
            Directory.CreateDirectory(tempDir);

            // Limite A3: nombre de fichiers + taille totale
            var maxFiles = _config.GetValue<int>("FileUpload:TempMaxFiles", 20);
            var maxMb    = _config.GetValue<long>("FileUpload:TempMaxSizeMb", 200);
            var existing = Directory.Exists(tempDir) ? new DirectoryInfo(tempDir).GetFiles() : Array.Empty<FileInfo>();
            var totalMb  = existing.Sum(f => f.Length) / (1024.0 * 1024.0);

            if (existing.Length >= maxFiles)
                return Json(new { success = false, error = $"Limite atteinte ({maxFiles} fichiers max par session)." });
            if (totalMb + file.Length / (1024.0 * 1024.0) > maxMb)
                return Json(new { success = false, error = $"Limite de taille atteinte ({maxMb} MB max par session)." });

            // Validation ext + taille + MIME
            var allowedExt   = _config.GetSection("FileUpload:AllowedExtensions").Get<string[]>() ?? Array.Empty<string>();
            var maxSizeBytes = _config.GetValue<int>("FileUpload:MaxSizeMb") * 1024L * 1024L;
            var ext          = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExt.Contains(ext))
                return Json(new { success = false, error = $"Extension '{ext}' non autorisée." });
            if (file.Length > maxSizeBytes)
                return Json(new { success = false, error = $"Fichier trop volumineux (max {_config.GetValue<int>("FileUpload:MaxSizeMb")} MB)." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var detectedMime = MimeHelper.DetectMime(ms);
            if (detectedMime == null || !MimeHelper.IsAllowed(ext, detectedMime))
                return Json(new { success = false, error = "Type MIME invalide." });

            var tempId   = Guid.NewGuid().ToString("N");
            var tempPath = Path.Combine(tempDir, tempId + ext);
            ms.Position  = 0;
            using (var fs = new FileStream(tempPath, FileMode.Create)) await ms.CopyToAsync(fs);

            return Json(new { success = true, tempId = tempId + ext,
                nomFichier = Path.GetFileNameWithoutExtension(file.FileName),
                fileSize = file.Length, fileExtension = ext });
        }

        // ── DeleteTemp (✕ hover pendant Create) ──────────────────
        [HttpDelete, ValidateAntiForgeryToken]
        public IActionResult DeleteTemp(string sessionGuid, string tempId)
        {
            if (string.IsNullOrWhiteSpace(sessionGuid) || string.IsNullOrWhiteSpace(tempId) ||
                !System.Text.RegularExpressions.Regex.IsMatch(sessionGuid, @"^[a-zA-Z0-9\-]+$") ||
                !System.Text.RegularExpressions.Regex.IsMatch(tempId, @"^[a-zA-Z0-9]+\.[a-z]+$"))
                return Json(new { success = false });

            var tempDir  = Path.Combine(_env.ContentRootPath,
                _config.GetValue<string>("FileUpload:UploadPath") ?? "uploads", "temp", sessionGuid);
            var filePath = Path.Combine(tempDir, tempId);

            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            return Json(new { success = true });
        }
    }
}
