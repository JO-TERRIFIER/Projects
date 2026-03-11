using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    public abstract class RbacControllerBase : Controller
    {
        protected readonly ApplicationDbContext Db;
        protected readonly IAuthorizationScopeService Scope;
        protected readonly IApprovalService Approvals;
        protected readonly IAuditService Audit;

        protected RbacControllerBase(ApplicationDbContext db, IAuthorizationScopeService scope, IApprovalService approvals, IAuditService audit)
        {
            Db = db;
            Scope = scope;
            Approvals = approvals;
            Audit = audit;
        }

        public override async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                int unreadCount = 0;
                var userId = CurrentUserId;

                if (IsSuperviseur || IsChefProjet)
                {
                    var unreadAlertsQuery = Db.NetworkAlerts.Where(a => !a.IsRead);
                    if (IsChefProjet && !IsSuperviseur)
                    {
                        var projIds = await Db.Projets.Where(p => p.ProjectManagerId == userId).Select(p => p.Id).ToListAsync();
                        
                        var allowedOltIds = await Db.Olts
                            .Where(o => projIds.Contains(o.Zone.ProjetId))
                            .Select(o => o.Id)
                            .ToListAsync();

                        unreadAlertsQuery = unreadAlertsQuery.Where(a => a.OltId.HasValue && allowedOltIds.Contains(a.OltId.Value));
                    }
                    unreadCount += await unreadAlertsQuery.CountAsync();
                    
                    var pendingReqsQuery = Db.ApprovalRequests.Where(r => r.Status == ApprovalStatus.Pending);
                    if (IsChefProjet && !IsSuperviseur)
                    {
                        var projIds = await Db.Projets.Where(p => p.ProjectManagerId == userId).Select(p => p.Id).ToListAsync();
                        pendingReqsQuery = pendingReqsQuery.Where(r => projIds.Contains(r.ProjetId));
                    }
                    unreadCount += await pendingReqsQuery.CountAsync();
                }

                if (IsTechTerrain || IsTechDessin)
                {
                    // For techs, count recently resolved approvals that might be 'unread'
                    // For simplicity, we just notify them if they have any decisions in the last 2 days
                    var recentCutoff = DateTime.UtcNow.AddDays(-2);
                    unreadCount += await Db.ApprovalRequests
                        .CountAsync(r => r.RequestedByUserId == userId && r.Status != ApprovalStatus.Pending && r.DecisionAt >= recentCutoff);
                }

                ViewBag.UnreadAlerts = unreadCount;
            }
            
            await base.OnActionExecutionAsync(context, next);
        }

        protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        protected bool IsSuperviseur => User.IsInRole(UserRoles.Superviseur);
        protected bool IsChefProjet => User.IsInRole(UserRoles.ChefProjet);
        protected bool IsTechTerrain => User.IsInRole(UserRoles.TechTerrain);
        protected bool IsTechDessin => User.IsInRole(UserRoles.TechDessin);
        protected bool IsVisiteur => User.IsInRole(UserRoles.Visiteur);

        protected async Task<bool> CanChefProjectAsync(int projetId)
        {
            if (IsSuperviseur) return true;
            if (!IsChefProjet) return false;
            return await Scope.IsAssignedToProjectAsync(CurrentUserId, projetId, AssignmentType.ChefProjet);
        }

        protected async Task<bool> CanTechTerrainProjectAsync(int projetId)
        {
            if (IsSuperviseur) return true;
            if (!IsTechTerrain) return false;
            return await Scope.IsAssignedToProjectAsync(CurrentUserId, projetId, AssignmentType.TechTerrain);
        }

        protected async Task<bool> CanTechDessinProjectAsync(int projetId)
        {
            if (IsSuperviseur) return true;
            if (!IsTechDessin) return false;
            return await Scope.IsAssignedToProjectAsync(CurrentUserId, projetId, AssignmentType.TechDessin);
        }

        protected static IActionResult? RequireText(string? text, string error, Func<IActionResult> onError)
            => string.IsNullOrWhiteSpace(text) ? onError() : null;

        protected async Task LogAsync(int? projetId, string actionType, string entityType, int? entityId, string description)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? CurrentUserId;
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await Audit.LogAsync(CurrentUserId, email, ip, projetId, actionType, entityType, entityId, description);
        }

        protected Task<int> ProjetIdFromZoneAsync(int zoneId)
            => Db.Zones.Where(z => z.Id == zoneId).Select(z => z.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromOltAsync(int oltId)
            => Db.Olts.Where(o => o.Id == oltId).Select(o => o.Zone.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromFdtAsync(int fdtId)
            => Db.Fdts.Where(f => f.Id == fdtId).Select(f => f.Olt.Zone.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromFatAsync(int fatId)
            => Db.Fats.Where(f => f.Id == fatId).Select(f => f.Fdt.Olt.Zone.ProjetId).FirstOrDefaultAsync();

        protected Task<int> ProjetIdFromBpiAsync(int bpiId)
            => Db.Bpis.Where(b => b.Id == bpiId).Select(b => b.Fdt.Olt.Zone.ProjetId).FirstOrDefaultAsync();
    }
}
