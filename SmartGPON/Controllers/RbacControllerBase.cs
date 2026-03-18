// ============================================================
// SmartGPON — Controllers/RbacControllerBase.cs — FRESH START
// ============================================================
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    public abstract class RbacControllerBase : Controller
    {
        protected readonly ApplicationDbContext Db;
        protected readonly IUserProjectAssignmentService Assignments;
        protected readonly IAuditLogService Audit;
        protected IConfiguration Configuration => HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        protected IWebHostEnvironment Env => HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        protected RbacControllerBase(ApplicationDbContext db, IUserProjectAssignmentService assignments, IAuditLogService audit)
        {
            Db = db;
            Assignments = assignments;
            Audit = audit;
        }

        public override async Task OnActionExecutionAsync(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                int unreadCount = 0;
                if (IsSuperviseur)
                {
                    unreadCount += await Db.ApprovalRequests.CountAsync(r => r.Status == 0); // Pending
                }
                else if (IsMembre)
                {
                    var projetIds = await Assignments.GetAccessibleProjetIdsAsync(User);
                    unreadCount += await Db.ApprovalRequests.CountAsync(r => r.Status == 0 && projetIds.Contains(r.ProjetId));
                }
                ViewBag.UnreadAlerts = unreadCount;
            }
            await base.OnActionExecutionAsync(context, next);
        }

        protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        protected bool IsSuperviseur => User.IsInRole(UserRoles.Superviseur);
        protected bool IsVisiteur => User.IsInRole(UserRoles.Visiteur);
        protected bool IsMembre => User.IsInRole(UserRoles.Membre);

        /// <summary>Returns HTTP 403 if current user is Visiteur (read-only).</summary>
        protected IActionResult? DenyVisiteur()
        {
            if (IsVisiteur) return Forbid();
            return null;
        }

        protected async Task<bool> CanWriteAsync(int projetId)
            => await Assignments.CanWriteProjectScopeAsync(User, projetId);

        protected async Task<List<int>> AccessibleProjetIdsAsync()
            => await Assignments.GetAccessibleProjetIdsAsync(User);

        protected async Task LogAsync(int? projetId, string actionType, string entityType, int? entityId, string description)
        {
            var user = await Db.Users.FindAsync(CurrentUserId);
            await Audit.LogAsync(CurrentUserId, projetId, actionType, entityType, entityId, description,
                user?.LastName, user?.FirstName);
        }

        protected Task<int> ProjetIdFromZoneAsync(int zoneId)
            => Db.Zones.Where(z => z.Id == zoneId).Select(z => z.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromOltAsync(int oltId)
            => Db.Olts.Where(o => o.Id == oltId).Select(o => o.Zone.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromFdtAsync(int fdtId)
            => Db.Fdts.Where(f => f.Id == fdtId).Select(f => f.Olt.Zone.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromBpiAsync(int bpiId)
            => Db.Bpis.Where(b => b.Id == bpiId).Select(b => b.Fdt.Olt.Zone.ProjetId).FirstOrDefaultAsync();

        // ── Helpers UploadTemp (partagés Projets + Zones) ────────────────
        protected string TempDir(string sessionGuid)
        {
            var uploadPath = Configuration.GetValue<string>("FileUpload:UploadPath") ?? "uploads";
            return Path.Combine(Env.ContentRootPath, uploadPath, "temp", sessionGuid);
        }

        /// <summary>
        /// Déplace les fichiers de /uploads/temp/{sessionGuid}/ vers leur destination finale
        /// et crée les enregistrements Resource en DB.
        /// </summary>
        protected async Task FinalizeTemp(string sessionGuid, int projetId, int? zoneId)
        {
            var tempDir = TempDir(sessionGuid);
            if (!Directory.Exists(tempDir)) return;

            var uploadPath = Configuration.GetValue<string>("FileUpload:UploadPath") ?? "uploads";
            var uploadRoot = Path.Combine(Env.ContentRootPath, uploadPath);

            foreach (var tempFile in Directory.GetFiles(tempDir))
            {
                var fileName  = Path.GetFileName(tempFile);
                var ext       = Path.GetExtension(fileName);
                var guid      = Path.GetFileNameWithoutExtension(fileName); // guid sans ext

                string relativePath = zoneId.HasValue
                    ? Path.Combine("projets", projetId.ToString(), "zones", zoneId.Value.ToString(), fileName)
                    : Path.Combine("projets", projetId.ToString(), fileName);

                var destFull = Path.Combine(uploadRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFull)!);
                System.IO.File.Move(tempFile, destFull, overwrite: true);

                var fi = new FileInfo(destFull);
                var resource = new Resource
                {
                    ProjetId         = projetId,
                    ZoneId           = zoneId,
                    NomFichier       = guid,                 // nom GUID (affiché peut être amélioré)
                    CheminFichier    = relativePath,
                    UploadedByUserId = CurrentUserId,
                    UploadedAt       = DateTime.UtcNow,
                    FileSize         = fi.Length,
                    FileExtension    = ext,
                    ContentType      = "application/octet-stream" // MIME pas redétecté ici
                };
                Db.Resources.Add(resource);
            }
            await Db.SaveChangesAsync();

            // Supprimer dossier temp
            try { Directory.Delete(tempDir, recursive: true); } catch { /* non bloquant */ }
        }
    }
}
