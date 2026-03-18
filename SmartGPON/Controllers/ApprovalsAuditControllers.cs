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

        public async Task<IActionResult> Index(int? projetId, string? action, string? entity, string? tech, string? from, string? to)
        {
            var q = Db.AuditLogs.AsQueryable();

            if (projetId.HasValue) q = q.Where(l => l.ProjetId == projetId.Value);
            if (!string.IsNullOrEmpty(action)) q = q.Where(l => l.ActionType != null && l.ActionType.ToLower().Contains(action.ToLower()));
            if (!string.IsNullOrEmpty(entity)) q = q.Where(l => l.EntityType != null && l.EntityType.ToLower().Contains(entity.ToLower()));
            if (!string.IsNullOrEmpty(tech)) q = q.Where(l =>
                (l.NomTech != null && l.NomTech.ToLower().Contains(tech.ToLower())) ||
                (l.PrenomTech != null && l.PrenomTech.ToLower().Contains(tech.ToLower())));
            if (DateTime.TryParse(from, out var dateFrom))
                q = q.Where(l => l.OccurredAt >= dateFrom.ToUniversalTime());
            if (DateTime.TryParse(to, out var dateTo))
                q = q.Where(l => l.OccurredAt <= dateTo.AddDays(1).ToUniversalTime());

            var logs = await q.OrderByDescending(l => l.OccurredAt).Take(200).ToListAsync();

            var list = logs.Select(l => new AuditLogDisplayVM
            {
                Id = l.Id, UserId = l.UserId, ProjetId = l.ProjetId,
                ActionType = l.ActionType, EntityType = l.EntityType, EntityId = l.EntityId,
                NomTech = l.NomTech, PrenomTech = l.PrenomTech,
                Description = l.Description, OccurredAt = l.OccurredAt
            }).ToList();

            // Pass filter values back for form persistence
            ViewBag.FilterAction = action;
            ViewBag.FilterEntity = entity;
            ViewBag.FilterTech = tech;
            ViewBag.FilterFrom = from;
            ViewBag.FilterTo = to;
            ViewBag.TotalCount = await Db.AuditLogs.CountAsync();

            return View(list);
        }
    }
}
