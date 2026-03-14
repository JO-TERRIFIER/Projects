// ============================================================
// SmartGPON — Controllers/NotificationsController.cs — FRESH START
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
    public class NotificationsController : RbacControllerBase
    {
        public NotificationsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index()
        {
            // Show recent audit logs as notifications
            var logs = await Audit.GetLogsAsync(pageSize: 30);
            var list = logs.Select(l => new AuditLogDisplayVM
            {
                Id = l.Id, UserId = l.UserId, ProjetId = l.ProjetId,
                ActionType = l.ActionType, EntityType = l.EntityType, EntityId = l.EntityId,
                NomTech = l.NomTech, PrenomTech = l.PrenomTech,
                Description = l.Description, OccurredAt = l.OccurredAt
            }).ToList();
            return View(list);
        }
    }
}
