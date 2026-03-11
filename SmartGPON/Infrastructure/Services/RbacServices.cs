using Microsoft.EntityFrameworkCore;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using SmartGPON.Core.Interfaces;
using SmartGPON.Infrastructure.Data;

namespace SmartGPON.Infrastructure.Services
{
    public class AuthorizationScopeService : IAuthorizationScopeService
    {
        private readonly ApplicationDbContext _db;
        public AuthorizationScopeService(ApplicationDbContext db) { _db = db; }

        public Task<bool> IsAssignedToProjectAsync(string userId, int projetId, AssignmentType assignmentType) =>
            _db.UserProjectAssignments.AsNoTracking().AnyAsync(a => a.UserId == userId && a.ProjetId == projetId && a.AssignmentType == assignmentType && a.IsActive);

        public Task<bool> IsAssignedToProjectAsync(string userId, int projetId) =>
            _db.UserProjectAssignments.AsNoTracking().AnyAsync(a => a.UserId == userId && a.ProjetId == projetId && a.IsActive);

        public async Task<bool> CanWriteProjectScopeAsync(System.Security.Claims.ClaimsPrincipal user, int projetId, bool allowTechTerrainCreate = false)
        {
            if (user.IsInRole(UserRoles.Superviseur)) return true;
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return false;

            if (user.IsInRole(UserRoles.ChefProjet))
            {
                return await IsAssignedToProjectAsync(userId, projetId, AssignmentType.ChefProjet);
            }

            if (allowTechTerrainCreate && user.IsInRole(UserRoles.TechTerrain))
            {
                return await IsAssignedToProjectAsync(userId, projetId, AssignmentType.TechTerrain);
            }

            return false;
        }
    }

    public class ApprovalService : IApprovalService
    {
        private readonly ApplicationDbContext _db;
        public ApprovalService(ApplicationDbContext db) { _db = db; }

        public async Task<ApprovalRequest> CreateAsync(int projetId, string requestedByUserId, string targetType, int? targetId, ApprovalActionType actionType, string reason)
        {
            var req = new ApprovalRequest
            {
                ProjetId = projetId,
                RequestedByUserId = requestedByUserId,
                TargetType = targetType,
                TargetId = targetId,
                ActionType = actionType,
                Reason = reason.Trim(),
                Status = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _db.ApprovalRequests.Add(req);
            await _db.SaveChangesAsync();
            return req;
        }

        public async Task<bool> ApproveAsync(int requestId, string decidedByUserId, string? comment)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null || req.Status != ApprovalStatus.Pending) return false;
            req.Status = ApprovalStatus.Approved;
            req.DecidedByUserId = decidedByUserId;
            req.DecisionAt = DateTime.UtcNow;
            req.DecisionComment = comment;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAsync(int requestId, string decidedByUserId, string? comment)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null || req.Status != ApprovalStatus.Pending) return false;
            req.Status = ApprovalStatus.Rejected;
            req.DecidedByUserId = decidedByUserId;
            req.DecisionAt = DateTime.UtcNow;
            req.DecisionComment = comment;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExecuteApprovedAsync(int requestId, string executorUserId)
        {
            var req = await _db.ApprovalRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null || req.Status != ApprovalStatus.Approved) return false;

            switch (req.ActionType)
            {
                case ApprovalActionType.DeleteProjet:
                    var p = await _db.Projets.FindAsync(req.TargetId);
                    if (p != null) _db.Projets.Remove(p);
                    break;
                case ApprovalActionType.DeleteZone:
                    var z = await _db.Zones.FindAsync(req.TargetId);
                    if (z != null) _db.Zones.Remove(z);
                    break;
                case ApprovalActionType.DeleteResource:
                    var r = await _db.Resources.FindAsync(req.TargetId);
                    if (r != null)
                    {
                        if (System.IO.File.Exists(r.CheminFichier)) System.IO.File.Delete(r.CheminFichier);
                        _db.Resources.Remove(r);
                    }
                    break;
                case ApprovalActionType.DeleteEquipment:
                    // Equipment execution can be module-specific; marked executed only by controller workflows.
                    break;
                case ApprovalActionType.EditEquipment:
                    break;
            }

            req.Status = ApprovalStatus.Executed;
            req.DecidedByUserId = executorUserId;
            req.DecisionAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        public AuditService(ApplicationDbContext db) { _db = db; }

        public async Task LogAsync(string userId, string userEmail, string? ipAddress, int? projetId, string actionType, string entityType, int? entityId, string description)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                IpAddress = ipAddress,
                ProjetId = projetId,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Description = description.Trim(),
                OccurredAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
