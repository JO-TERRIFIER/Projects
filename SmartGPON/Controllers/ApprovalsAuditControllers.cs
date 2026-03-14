// ============================================================
// SmartGPON — Controllers/ApprovalsAuditControllers.cs — FRESH START
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using SmartGPON.Web.ViewModels;

namespace SmartGPON.Web.Controllers
{
    // ── Approvals ───────────────────────────────────────────
    [Authorize]
    public class ApprovalsController : RbacControllerBase
    {
        private readonly IApprovalService _approvals;
        public ApprovalsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au, IApprovalService approvals)
            : base(db, a, au)
        { _approvals = approvals; }

        public async Task<IActionResult> Index()
        {
            var pending = await _approvals.GetPendingAsync();
            var list = pending.Select(r => new ApprovalRequestDisplayVM
            {
                Id = r.Id, ProjetId = r.ProjetId, ProjetNom = r.Projet?.Nom ?? "",
                RequestedByUserId = r.RequestedByUserId,
                TargetType = r.TargetType, ActionType = r.ActionType, Status = r.Status
            }).ToList();
            return View(list);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            await _approvals.ApproveAsync(id);
            TempData["Success"] = "Demande approuvée.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var d = DenyVisiteur(); if (d != null) return d;
            await _approvals.RejectAsync(id);
            TempData["Success"] = "Demande rejetée.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── AuditLogs ───────────────────────────────────────────
    [Authorize]
    public class AuditLogsController : RbacControllerBase
    {
        public AuditLogsController(ApplicationDbContext db, IUserProjectAssignmentService a, IAuditLogService au)
            : base(db, a, au) { }

        public async Task<IActionResult> Index(int? projetId)
        {
            var logs = await Audit.GetLogsAsync(projetId);
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
