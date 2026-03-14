// ============================================================
// SmartGPON — Core/Interfaces/IRbacServices.cs — FRESH START
// ============================================================
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;
using System.Security.Claims;

namespace SmartGPON.Core.Interfaces
{
    // ── UserProjectAssignment ───────────────────────────────
    public interface IUserProjectAssignmentService
    {
        /// <summary>Get all assignments for a project.</summary>
        Task<List<UserProjectAssignment>> GetByProjetAsync(int projetId);

        /// <summary>Get all assignments for a user.</summary>
        Task<List<UserProjectAssignment>> GetByUserAsync(string userId);

        /// <summary>Check if user is assigned to project with any role.</summary>
        Task<bool> IsAssignedToProjectAsync(string userId, int projetId);

        /// <summary>Check if user is assigned to project with a specific AssignmentType.</summary>
        Task<bool> IsAssignedToProjectAsync(string userId, int projetId, AssignmentType assignmentType);

        /// <summary>Determine if ClaimsPrincipal can write to project scope.</summary>
        Task<bool> CanWriteProjectScopeAsync(ClaimsPrincipal user, int projetId);

        /// <summary>Get all projetIds accessible to user based on role.</summary>
        Task<List<int>> GetAccessibleProjetIdsAsync(ClaimsPrincipal user);

        Task<UserProjectAssignment> CreateAsync(UserProjectAssignment assignment);
        Task DeleteAsync(int id);
    }

    // ── AuditLog ────────────────────────────────────────────
    public interface IAuditLogService
    {
        Task LogAsync(string? userId, int? projetId, string actionType,
                      string entityType, int? entityId, string description,
                      string? nomTech = null, string? prenomTech = null);

        Task<List<AuditLog>> GetLogsAsync(int? projetId = null, int page = 1, int pageSize = 50);
    }

    // ── Approval ────────────────────────────────────────────
    public interface IApprovalService
    {
        Task<List<ApprovalRequest>> GetByProjetAsync(int projetId);
        Task<List<ApprovalRequest>> GetPendingAsync();
        Task<ApprovalRequest> CreateAsync(ApprovalRequest request);
        Task ApproveAsync(int id);
        Task RejectAsync(int id);
    }
}
