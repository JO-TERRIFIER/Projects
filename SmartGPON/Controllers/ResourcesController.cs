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
    public class ResourcesController : RbacControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] SuperviseurChefExtensions = { ".dwg", ".pdf", ".png", ".jpg", ".jpeg", ".xlsx" };
        private static readonly string[] TechDessinExtensions = { ".dwg" };

        public ResourcesController(ApplicationDbContext db, IWebHostEnvironment env, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
            : base(db, scope, approvals, audit)
        {
            _env = env;
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet,TechDessin")]
        public async Task<IActionResult> Upload(int? zoneId, int? projetId, IFormFile file, string uploadDescription)
        {
            if (string.IsNullOrWhiteSpace(uploadDescription)) { TempData["Error"] = "Description upload obligatoire."; return Back(zoneId, projetId); }
            if (file == null || file.Length == 0) { TempData["Error"] = "Fichier vide."; return Back(zoneId, projetId); }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = (IsTechDessin && !IsSuperviseur && !IsChefProjet) ? TechDessinExtensions : SuperviseurChefExtensions;
            if (!allowed.Contains(ext)) { TempData["Error"] = $"Type {ext} non autorisé pour votre profil."; return Back(zoneId, projetId); }

            int resolvedProjetId = projetId ?? 0;
            if (zoneId.HasValue)
            {
                resolvedProjetId = await ProjetIdFromZoneAsync(zoneId.Value);
                if (resolvedProjetId == 0) { TempData["Error"] = "Zone invalide."; return Back(zoneId, projetId); }
            }

            if (IsChefProjet && !IsSuperviseur)
            {
                if (resolvedProjetId == 0 || !await CanChefProjectAsync(resolvedProjetId)) return Forbid();
            }

            if (IsTechDessin && !IsSuperviseur && !IsChefProjet)
            {
                if (!zoneId.HasValue || ext != ".dwg") return Forbid();
                if (!await CanTechDessinProjectAsync(resolvedProjetId)) return Forbid();
            }

            var folder = zoneId.HasValue
                ? Path.Combine(_env.WebRootPath, "resources", "zones", zoneId.ToString()!)
                : Path.Combine(_env.WebRootPath, "resources", "projets", projetId.ToString()!);
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var resource = new Resource
            {
                ZoneId = zoneId,
                ProjetId = projetId,
                NomFichier = file.FileName,
                CheminFichier = filePath,
                TypeFichier = ext,
                TailleFichier = file.Length
            };
            Db.Resources.Add(resource);
            await Db.SaveChangesAsync();
            await LogAsync(resolvedProjetId == 0 ? projetId : resolvedProjetId, "Upload", nameof(Resource), resource.Id, uploadDescription);

            TempData["Success"] = "Fichier uploadé.";
            return Back(zoneId, projetId);
        }

        public async Task<IActionResult> Download(int id)
        {
            var r = await Db.Resources.FindAsync(id);
            if (r == null || !System.IO.File.Exists(r.CheminFichier)) return NotFound();
            return PhysicalFile(r.CheminFichier, "application/octet-stream", r.NomFichier);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Superviseur,ChefProjet,TechDessin")]
        public async Task<IActionResult> Delete(int id, string reason)
        {
            var r = await Db.Resources.FindAsync(id);
            if (r == null) return NotFound();
            if (string.IsNullOrWhiteSpace(reason)) { TempData["Error"] = "Raison obligatoire."; return Back(r.ZoneId, r.ProjetId); }

            var projetId = r.ProjetId ?? await Db.Zones.Where(z => z.Id == r.ZoneId).Select(z => z.ProjetId).FirstOrDefaultAsync();

            if (IsSuperviseur || (IsChefProjet && await CanChefProjectAsync(projetId)))
            {
                if (System.IO.File.Exists(r.CheminFichier)) System.IO.File.Delete(r.CheminFichier);
                Db.Resources.Remove(r);
                await Db.SaveChangesAsync();
                await LogAsync(projetId, "DeleteFile", nameof(Resource), r.Id, reason);
                TempData["Success"] = "Fichier supprimé.";
                return Back(r.ZoneId, r.ProjetId);
            }

            if (IsTechDessin && await CanTechDessinProjectAsync(projetId))
            {
                await Approvals.CreateAsync(projetId, CurrentUserId, nameof(Resource), r.Id, ApprovalActionType.DeleteResource, reason);
                await LogAsync(projetId, "RequestDeleteFile", nameof(Resource), r.Id, reason);
                TempData["Success"] = "Demande de suppression envoyée au ChefProjet.";
                return Back(r.ZoneId, r.ProjetId);
            }

            return Forbid();
        }

        private IActionResult Back(int? zoneId, int? projetId)
        {
            if (zoneId.HasValue) return RedirectToAction("Index", "Zones");
            if (projetId.HasValue) return RedirectToAction("Index", "Projets");
            return RedirectToAction("Index", "Home");
        }
    }
}
