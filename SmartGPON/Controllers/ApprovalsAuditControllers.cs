using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    // ─────────────────────────────────────────────────────────────────────────
    // APPROVALS
    // ─────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Superviseur,ChefProjet")]
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ApprovalsController(ApplicationDbContext db) { _db = db; }

        private void AddAuditLog(string actionType, string entityType, int? entityId, int? projetId, string description)
        {
            var uid   = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? uid;
            var ip    = HttpContext.Connection.RemoteIpAddress?.ToString();
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = uid, UserEmail = email, IpAddress = ip,
                ProjetId = projetId, ActionType = actionType,
                EntityType = entityType, EntityId = entityId,
                Description = description, OccurredAt = DateTime.UtcNow
            });
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            IQueryable<ApprovalRequest> query = _db.ApprovalRequests.AsNoTracking().Include(r => r.Projet);
            if (User.IsInRole(UserRoles.ChefProjet) && !User.IsInRole(UserRoles.Superviseur))
            {
                var projectIds = await _db.UserProjectAssignments.AsNoTracking()
                    .Where(a => a.UserId == userId && a.AssignmentType == AssignmentType.ChefProjet && a.IsActive)
                    .Select(a => a.ProjetId).ToListAsync();
                query = query.Where(r => projectIds.Contains(r.ProjetId));
            }
            return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? comment)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null || req.Status != ApprovalStatus.Pending) return RedirectToAction(nameof(Index));
            if (string.IsNullOrWhiteSpace(comment)) { TempData["Error"] = "Description obligatoire."; return RedirectToAction(nameof(Index)); }

            req.Status = ApprovalStatus.Approved;
            req.DecidedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            req.DecisionAt = DateTime.UtcNow;
            req.DecisionComment = comment;

            if (req.ActionType == ApprovalActionType.DeleteProjet && User.IsInRole(UserRoles.Superviseur))
            {
                var p = await _db.Projets.FindAsync(req.TargetId);
                if (p != null) _db.Projets.Remove(p);
                req.Status = ApprovalStatus.Executed;
            }
            else if (req.ActionType == ApprovalActionType.DeleteZone && User.IsInRole(UserRoles.Superviseur))
            {
                var z = await _db.Zones.FindAsync(req.TargetId);
                if (z != null) _db.Zones.Remove(z);
                req.Status = ApprovalStatus.Executed;
            }
            else if (req.ActionType == ApprovalActionType.DeleteResource)
            {
                var r = await _db.Resources.FindAsync(req.TargetId);
                if (r != null)
                {
                    if (System.IO.File.Exists(r.CheminFichier)) System.IO.File.Delete(r.CheminFichier);
                    _db.Resources.Remove(r);
                    req.Status = ApprovalStatus.Executed;
                }
            }

            AddAuditLog("Approve", nameof(ApprovalRequest), req.Id, req.ProjetId, comment.Trim());
            await _db.SaveChangesAsync();
            TempData["Success"] = "Demande approuvée.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? comment)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null || req.Status != ApprovalStatus.Pending) return RedirectToAction(nameof(Index));
            if (string.IsNullOrWhiteSpace(comment)) { TempData["Error"] = "Description obligatoire."; return RedirectToAction(nameof(Index)); }

            req.Status = ApprovalStatus.Rejected;
            req.DecidedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            req.DecisionAt = DateTime.UtcNow;
            req.DecisionComment = comment;

            AddAuditLog("Reject", nameof(ApprovalRequest), req.Id, req.ProjetId, comment.Trim());
            await _db.SaveChangesAsync();
            TempData["Success"] = "Demande rejetée.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIT VIEW MODEL
    // ─────────────────────────────────────────────────────────────────────────
    public class AuditViewModel
    {
        public int Id { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? ProjetNom { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIT CONTROLLER
    // ─────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Superviseur,ChefProjet")]
    public class AuditController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly string _connStr;

        public AuditController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _connStr = config.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("DefaultConnection not configured");
        }

        public async Task<IActionResult> Index(string? action = null, string? entity = null)
        {
            var vms = new List<AuditViewModel>();

            // ── RBAC: build WHERE conditions ──────────────────────────────
            var conditions = new List<string>();
            var sqlParams  = new List<Microsoft.Data.SqlClient.SqlParameter>();

            if (User.IsInRole(UserRoles.ChefProjet) && !User.IsInRole(UserRoles.Superviseur))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var myProjectIds  = await _db.Projets
                    .Where(p => p.ProjectManagerId == currentUserId)
                    .Select(p => p.Id).ToListAsync();

                if (myProjectIds.Any())
                {
                    var idList = string.Join(",", myProjectIds);
                    conditions.Add($"(a.ProjetId IN ({idList}) OR a.UserId = @uid)");
                }
                else
                {
                    conditions.Add("a.UserId = @uid");
                }
                sqlParams.Add(new Microsoft.Data.SqlClient.SqlParameter("@uid", currentUserId));
            }
            // Superviseur → no restriction (reads all rows)

            if (!string.IsNullOrWhiteSpace(action))
            {
                conditions.Add("a.ActionType = @act");
                sqlParams.Add(new Microsoft.Data.SqlClient.SqlParameter("@act", action));
            }
            if (!string.IsNullOrWhiteSpace(entity))
            {
                conditions.Add("a.EntityType = @ent");
                sqlParams.Add(new Microsoft.Data.SqlClient.SqlParameter("@ent", entity));
            }

            var where = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
            var sql   = $@"SELECT TOP 500
                              a.Id, a.UserEmail, a.IpAddress, a.ProjetId,
                              a.ActionType, a.EntityType, a.EntityId,
                              a.Description, a.OccurredAt,
                              p.Nom AS ProjetNom
                           FROM AuditLogs a
                           LEFT JOIN Projets p ON p.Id = a.ProjetId
                           {where}
                           ORDER BY a.OccurredAt DESC";

            // Fresh, independent connection — avoids all EF connection sharing
            await using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connStr);
            await conn.OpenAsync();
            await using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn);
            foreach (var p in sqlParams) cmd.Parameters.Add(p);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vms.Add(new AuditViewModel
                {
                    Id          = reader.GetInt32(0),
                    UserEmail   = reader.IsDBNull(1) ? "—"   : reader.GetString(1),
                    IpAddress   = reader.IsDBNull(2) ? null  : reader.GetString(2),
                    ProjetNom   = reader.IsDBNull(9) ? null  : reader.GetString(9),
                    ActionType  = reader.IsDBNull(4) ? ""    : reader.GetString(4),
                    EntityType  = reader.IsDBNull(5) ? ""    : reader.GetString(5),
                    EntityId    = reader.IsDBNull(6) ? null  : (int?)reader.GetInt32(6),
                    Description = reader.IsDBNull(7) ? ""    : reader.GetString(7),
                    OccurredAt  = reader.GetDateTime(8)
                });
            }

            ViewBag.CurrentAction = action;
            ViewBag.CurrentEntity = entity;
            ViewBag.Actions  = vms.Select(l => l.ActionType).Distinct().OrderBy(a => a).ToList();
            ViewBag.Entities = vms.Select(l => l.EntityType).Distinct().OrderBy(e => e).ToList();

            return View(vms);
        }
    }
}
