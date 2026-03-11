using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;

namespace SmartGPON.Core.Interfaces
{
    public interface IAuthorizationScopeService
    {
        Task<bool> IsAssignedToProjectAsync(string userId, int projetId, AssignmentType assignmentType);
        Task<bool> IsAssignedToProjectAsync(string userId, int projetId);
        Task<bool> CanWriteProjectScopeAsync(System.Security.Claims.ClaimsPrincipal user, int projetId, bool allowTechTerrainCreate = false);
    }

    public interface IApprovalService
    {
        Task<ApprovalRequest> CreateAsync(int projetId, string requestedByUserId, string targetType, int? targetId, ApprovalActionType actionType, string reason);
        Task<bool> ApproveAsync(int requestId, string decidedByUserId, string? comment);
        Task<bool> RejectAsync(int requestId, string decidedByUserId, string? comment);
        Task<bool> ExecuteApprovedAsync(int requestId, string executorUserId);
    }

    public interface IAuditService
    {
        Task LogAsync(string userId, string userEmail, string? ipAddress, int? projetId, string actionType, string entityType, int? entityId, string description);
    }
}
