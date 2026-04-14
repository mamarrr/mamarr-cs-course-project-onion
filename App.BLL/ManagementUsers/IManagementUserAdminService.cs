using App.Domain;
using App.Domain.Identity;

namespace App.BLL.ManagementUsers;

/// <summary>
/// Service for managing company users within a management company scope.
/// Enforces tenant isolation and role-based authorization.
/// </summary>
public interface IManagementUserAdminService
{
    /// <summary>
    /// Resolves actor authorization for the given company slug.
    /// Returns authorized actor context if the user has OWNER or MANAGER role.
    /// </summary>
    Task<ManagementUserAdminAuthorizationResult> AuthorizeAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all company members for the authorized company context.
    /// </summary>
    Task<ManagementUserListResult> ListCompanyMembersAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single membership for editing, scoped to the authorized company.
    /// </summary>
    Task<ManagementUserEditResult> GetMembershipForEditAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new company user by email for the authorized company context.
    /// </summary>
    Task<ManagementUserAddResult> AddUserByEmailAsync(
        ManagementUserAdminAuthorizedContext context,
        ManagementUserAddRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing company membership within the authorized company scope.
    /// </summary>
    Task<ManagementUserUpdateResult> UpdateMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        ManagementUserUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes (or deactivates) a company membership within the authorized company scope.
    /// </summary>
    Task<ManagementUserDeleteResult> DeleteMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending access requests for the company.
    /// Currently returns empty placeholder; will be implemented with onboarding workflow.
    /// </summary>
    Task<PendingAccessRequestListResult> GetPendingAccessRequestsAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a pending access request in the authorized company context.
    /// </summary>
    Task<PendingAccessRequestActionResult> ApprovePendingAccessRequestAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a pending access request in the authorized company context.
    /// </summary>
    Task<PendingAccessRequestActionResult> RejectPendingAccessRequestAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available management company roles for dropdown selection.
    /// </summary>
    Task<IReadOnlyList<ManagementCompanyRole>> GetAvailableRolesAsync(
        CancellationToken cancellationToken = default);
}
