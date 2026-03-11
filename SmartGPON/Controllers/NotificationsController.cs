using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Enums;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public NotificationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isSuperviseur = User.IsInRole(UserRoles.Superviseur);
            var isChefProjet = User.IsInRole(UserRoles.ChefProjet);
            var isTech = User.IsInRole(UserRoles.TechTerrain) || User.IsInRole(UserRoles.TechDessin);

            var notifications = new List<NotificationItem>();

            if (isSuperviseur || isChefProjet)
            {
                // Alertes RÃ©seau Non Lues
                var unreadAlertsQuery = _db.NetworkAlerts.AsNoTracking().Where(a => !a.IsRead);
                if (isChefProjet && !isSuperviseur)
                {
                    var projIds = await _db.Projets.AsNoTracking().Where(p => p.ProjectManagerId == userId).Select(p => p.Id).ToListAsync();
                    
                    var allowedOltIds = await _db.Olts.AsNoTracking()
                        .Where(o => projIds.Contains(o.Zone.ProjetId))
                        .Select(o => o.Id)
                        .ToListAsync();

                    unreadAlertsQuery = unreadAlertsQuery.Where(a => a.OltId.HasValue && allowedOltIds.Contains(a.OltId.Value));
                }
                
                var unreadAlerts = await unreadAlertsQuery
                    .OrderByDescending(a => a.DateAlerte)
                    .Take(20)
                    .ToListAsync();

                notifications.AddRange(unreadAlerts.Select(a => new NotificationItem
                {
                    Type = "Alerte SÃ©curitÃ©",
                    Titre = a.Titre,
                    Date = a.DateAlerte,
                    Lien = "/Security/Index",
                    Icon = "bi-shield-exclamation",
                    Color = "var(--accent-red)",
                    IsUnread = true
                }));

                // Demandes d'approbation en attente
                var pendingReqsQuery = _db.ApprovalRequests.AsNoTracking().Where(r => r.Status == ApprovalStatus.Pending);
                if (isChefProjet && !isSuperviseur)
                {
                    var projIds = await _db.Projets.AsNoTracking().Where(p => p.ProjectManagerId == userId).Select(p => p.Id).ToListAsync();
                    pendingReqsQuery = pendingReqsQuery.Where(r => projIds.Contains(r.ProjetId));
                }
                
                var pendingReqs = await pendingReqsQuery.OrderByDescending(r => r.CreatedAt).Take(20).ToListAsync();
                notifications.AddRange(pendingReqs.Select(r => new NotificationItem
                {
                    Type = "Demande (En Attente)",
                    Titre = $"{r.TargetId} ({r.TargetType}) - {r.ActionType}",
                    Date = r.CreatedAt,
                    Lien = "/Approvals/Index",
                    Icon = "bi-question-circle",
                    Color = "var(--accent-amber)",
                    IsUnread = true
                }));
            }
            
            if (isTech || isSuperviseur || isChefProjet)
            {
                // Retours d'approbation rÃ©cents initiÃ©s par l'utilisateur
                var recentDecisions = await _db.ApprovalRequests.AsNoTracking()
                    .Where(r => r.RequestedByUserId == userId && r.Status != ApprovalStatus.Pending)
                    .OrderByDescending(r => r.DecisionAt)
                    .Take(20)
                    .ToListAsync();

                notifications.AddRange(recentDecisions.Select(r => new NotificationItem
                {
                    Type = "Retour demande",
                    Titre = $"{r.TargetId} ({r.TargetType}) - {r.Status}",
                    Date = r.DecisionAt ?? r.CreatedAt,
                    Lien = "#",
                    Icon = r.Status == ApprovalStatus.Approved ? "bi-check-circle" : (r.Status == ApprovalStatus.Rejected ? "bi-x-circle" : "bi-info-circle"),
                    Color = r.Status == ApprovalStatus.Approved ? "var(--accent-green)" : (r.Status == ApprovalStatus.Rejected ? "var(--accent-red)" : "var(--accent-cyan)"),
                    IsUnread = false // Could track read state if needed, but keeping it simple
                }));
            }

            // Trier par date
            return View(notifications.OrderByDescending(n => n.Date).ToList());
        }
    }

    public class NotificationItem
    {
        public string Type { get; set; } = string.Empty;
        public string Titre { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Lien { get; set; } = "#";
        public string Icon { get; set; } = "bi-bell";
        public string Color { get; set; } = "var(--accent-cyan)";
        public bool IsUnread { get; set; }
    }
}
