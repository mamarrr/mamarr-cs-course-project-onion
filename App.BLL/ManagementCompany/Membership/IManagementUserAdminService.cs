using App.Domain;

namespace App.BLL.ManagementCompany.Membership;

/// <summary>
/// Service for managing company users within a management company scope.
/// Enforces tenant isolation and role-based authorization.
/// </summary>
public interface IManagementUserAdminService
{
    /// <summary>
    /// Resolves management-area access for the given company slug.
    /// Returns actor context if the user is an effective company member.
    /// </summary>
    Task<ManagementAreaAuthorizationResult> AuthorizeManagementAreaAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves actor authorization for management user administration in the given company slug.
    /// Returns authorized actor context if the user has effective OWNER or MANAGER role.
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
    /// Gets role options for the generic add-user flow, filtered by actor capabilities.
    /// </summary>
    Task<IReadOnlyList<ManagementRoleOption>> GetAddRoleOptionsAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role options for the generic edit-user flow, filtered by actor capabilities and target membership.
    /// </summary>
    Task<ManagementRoleOptionsResult> GetEditRoleOptionsAsync(
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
    /// Deletes a company membership within the authorized company scope.
    /// </summary>
    Task<ManagementUserDeleteResult> DeleteMembershipAsync(
        ManagementUserAdminAuthorizedContext context,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists ownership transfer candidates for the current owner.
    /// </summary>
    Task<OwnershipTransferCandidateListResult> GetOwnershipTransferCandidatesAsync(
        ManagementUserAdminAuthorizedContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transfers ownership from the current owner to another existing company member.
    /// </summary>
    Task<OwnershipTransferResult> TransferOwnershipAsync(
        ManagementUserAdminAuthorizedContext context,
        TransferOwnershipRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all management company roles.
    /// Kept temporarily for existing web-layer callers until they are migrated to actor-aware role option APIs.
    /// </summary>
    Task<IReadOnlyList<ManagementCompanyRole>> GetAvailableRolesAsync(
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
}
