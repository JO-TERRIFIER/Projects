// ============================================================
// SmartGPON — Infrastructure/Services/RbacServices.cs — FRESH START
// ============================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;
using System.Security.Claims;

namespace SmartGPON.Infrastructure.Services
{
    // ── UserProjectAssignmentService ────────────────────────
    public class UserProjectAssignmentService : IUserProjectAssignmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UserProjectAssignmentService> _log;

        public UserProjectAssignmentService(ApplicationDbContext db, ILogger<UserProjectAssignmentService> log)
        { _db = db; _log = log; }

        public async Task<List<UserProjectAssignment>> GetByProjetAsync(int projetId)
        {
            try { return await _db.UserProjectAssignments.Where(a => a.ProjetId == projetId && a.IsActive).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetByProjetAsync error"); throw; }
        }

        public async Task<List<UserProjectAssignment>> GetByUserAsync(string userId)
        {
            try { return await _db.UserProjectAssignments.Where(a => a.UserId == userId && a.IsActive).Include(a => a.Projet).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetByUserAsync error"); throw; }
        }

        public async Task<bool> IsAssignedToProjectAsync(string userId, int projetId)
        {
            try { return await _db.UserProjectAssignments.AnyAsync(a => a.UserId == userId && a.ProjetId == projetId && a.IsActive); }
            catch (Exception ex) { _log.LogError(ex, "IsAssignedToProjectAsync error"); throw; }
        }

        public async Task<bool> IsAssignedToProjectAsync(string userId, int projetId, AssignmentType assignmentType)
        {
            try { return await _db.UserProjectAssignments.AnyAsync(a => a.UserId == userId && a.ProjetId == projetId && a.AssignmentType == assignmentType && a.IsActive); }
            catch (Exception ex) { _log.LogError(ex, "IsAssignedToProjectAsync error"); throw; }
        }

        public async Task<bool> CanWriteProjectScopeAsync(ClaimsPrincipal user, int projetId)
        {
            try
            {
                // Superviseur → ALL projects
                if (user.IsInRole(UserRoles.Superviseur)) return true;
                // Visiteur → READ ONLY
                if (user.IsInRole(UserRoles.Visiteur)) return false;
                // Membre → via UserProjectAssignments
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return false;
                return await IsAssignedToProjectAsync(userId, projetId);
            }
            catch (Exception ex) { _log.LogError(ex, "CanWriteProjectScopeAsync error"); throw; }
        }

        public async Task<List<int>> GetAccessibleProjetIdsAsync(ClaimsPrincipal user)
        {
            try
            {
                // Superviseur → ALL
                if (user.IsInRole(UserRoles.Superviseur))
                    return await _db.Projets.Select(p => p.Id).ToListAsync();

                // Visiteur → filter by ClientId
                if (user.IsInRole(UserRoles.Visiteur))
                {
                    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId)) return new List<int>();
                    var appUser = await _db.Users.FindAsync(userId);
                    if (appUser?.ClientId == null) return new List<int>();
                    return await _db.Projets.Where(p => p.ClientId == appUser.ClientId).Select(p => p.Id).ToListAsync();
                }

                // Membre → via assignments
                {
                    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId)) return new List<int>();
                    return await _db.UserProjectAssignments
                        .Where(a => a.UserId == userId && a.IsActive)
                        .Select(a => a.ProjetId).Distinct().ToListAsync();
                }
            }
            catch (Exception ex) { _log.LogError(ex, "GetAccessibleProjetIdsAsync error"); throw; }
        }

        public async Task<UserProjectAssignment> CreateAsync(UserProjectAssignment assignment)
        {
            try { _db.UserProjectAssignments.Add(assignment); await _db.SaveChangesAsync(); return assignment; }
            catch (Exception ex) { _log.LogError(ex, "CreateAsync error"); throw; }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var e = await _db.UserProjectAssignments.FindAsync(id);
                if (e != null) { _db.UserProjectAssignments.Remove(e); await _db.SaveChangesAsync(); }
            }
            catch (Exception ex) { _log.LogError(ex, "DeleteAsync error"); throw; }
        }
    }

    // ── AuditLogService ─────────────────────────────────────
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AuditLogService> _log;

        public AuditLogService(ApplicationDbContext db, ILogger<AuditLogService> log)
        { _db = db; _log = log; }

        public async Task LogAsync(string? userId, int? projetId, string actionType,
            string entityType, int? entityId, string description,
            string? nomTech = null, string? prenomTech = null)
        {
            try
            {
                _db.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    ProjetId = projetId,
                    ActionType = actionType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    NomTech = nomTech,
                    PrenomTech = prenomTech,
                    OccurredAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "LogAsync error"); throw; }
        }

        public async Task<List<AuditLog>> GetLogsAsync(int? projetId = null, int page = 1, int pageSize = 50)
        {
            try
            {
                var q = _db.AuditLogs.AsQueryable();
                if (projetId.HasValue) q = q.Where(a => a.ProjetId == projetId.Value);
                return await q.OrderByDescending(a => a.OccurredAt)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }
            catch (Exception ex) { _log.LogError(ex, "GetLogsAsync error"); throw; }
        }
    }

    // ── ApprovalService ─────────────────────────────────────
    public class ApprovalService : IApprovalService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ApprovalService> _log;

        public ApprovalService(ApplicationDbContext db, ILogger<ApprovalService> log)
        { _db = db; _log = log; }

        public async Task<List<ApprovalRequest>> GetByProjetAsync(int projetId)
        {
            try { return await _db.ApprovalRequests.Where(a => a.ProjetId == projetId).OrderByDescending(a => a.Id).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetByProjetAsync error"); throw; }
        }

        public async Task<List<ApprovalRequest>> GetPendingAsync()
        {
            try { return await _db.ApprovalRequests.Where(a => a.Status == 0).Include(a => a.Projet).OrderByDescending(a => a.Id).ToListAsync(); }
            catch (Exception ex) { _log.LogError(ex, "GetPendingAsync error"); throw; }
        }

        public async Task<ApprovalRequest> CreateAsync(ApprovalRequest request)
        {
            try { _db.ApprovalRequests.Add(request); await _db.SaveChangesAsync(); return request; }
            catch (Exception ex) { _log.LogError(ex, "CreateAsync error"); throw; }
        }

        public async Task ApproveAsync(int id)
        {
            try
            {
                var r = await _db.ApprovalRequests.FindAsync(id);
                if (r != null) { r.Status = 1; await _db.SaveChangesAsync(); }
            }
            catch (Exception ex) { _log.LogError(ex, "ApproveAsync error"); throw; }
        }

        public async Task RejectAsync(int id)
        {
            try
            {
                var r = await _db.ApprovalRequests.FindAsync(id);
                if (r != null) { r.Status = 2; await _db.SaveChangesAsync(); }
            }
            catch (Exception ex) { _log.LogError(ex, "RejectAsync error"); throw; }
        }
    }
}
