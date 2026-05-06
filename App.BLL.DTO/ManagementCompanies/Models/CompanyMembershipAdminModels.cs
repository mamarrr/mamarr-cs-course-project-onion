namespace App.BLL.DTO.ManagementCompanies.Models;

public enum CompanyMembershipAuthorizationFailureReason
{
    None = 0,
    CompanyNotFound,
    MembershipNotFound,
    MembershipInactive,
    MembershipNotEffective,
    InsufficientPrivileges
}

public enum CompanyMembershipUserActionBlockReason
{
    None = 0,
    OwnerProtected,
    SelfProtected,
    OwnershipTransferRequired,
    RoleNotAssignable,
    MembershipNotEffective,
    InvalidDateRange,
    TargetNotEligibleForOwnership
}

/// <summary>
/// Effective management membership context.
/// </summary>
public class CompanyMembershipContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid ActorMembershipId { get; set; }
    public Guid ActorRoleId { get; set; }
    public string ActorRoleCode { get; set; } = default!;
    public string ActorRoleLabel { get; set; } = default!;
    public bool IsOwner { get; set; }
    public bool IsAdmin { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

/// <summary>
/// Authorized context for performing management user administration operations.
/// </summary>
public class CompanyAdminAuthorizedContext : CompanyMembershipContext
{
}

/// <summary>
/// Result of listing company members.
/// </summary>
public class CompanyMembershipListResult
{
    public IReadOnlyList<CompanyMembershipUserListItem> Members { get; set; } = Array.Empty<CompanyMembershipUserListItem>();
}

/// <summary>
/// Single member item in the company users list.
/// </summary>
public class CompanyMembershipUserListItem
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActor { get; set; }
    public bool IsOwner { get; set; }
    public bool IsEffective { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanTransferOwnership { get; set; }
    public bool CanChangeRole { get; set; }
    public bool CanDeactivate { get; set; }
    public string? ProtectedReason { get; set; }
    public CompanyMembershipUserActionBlockReason ProtectedReasonCode { get; set; }
}

/// <summary>
/// Model for editing a membership.
/// </summary>
public class CompanyMembershipEditModel
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsOwner { get; set; }
    public bool IsActor { get; set; }
    public bool IsEffective { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanTransferOwnership { get; set; }
    public bool CanChangeRole { get; set; }
    public bool CanDeactivate { get; set; }
    public bool OwnershipTransferRequired { get; set; }
    public string? ProtectedReason { get; set; }
    public CompanyMembershipUserActionBlockReason ProtectedReasonCode { get; set; }
    public IReadOnlyList<CompanyMembershipRoleOption> AvailableRoleOptions { get; set; } = Array.Empty<CompanyMembershipRoleOption>();
}

public class CompanyMembershipRoleOption
{
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
}

/// <summary>
/// Request to add a user by email.
/// </summary>
public class CompanyMembershipAddRequest
{
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    
}

/// <summary>
/// Request to update a membership.
/// </summary>
public class CompanyMembershipUpdateRequest
{
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class TransferOwnershipRequest
{
    public Guid TargetMembershipId { get; set; }
}

public class OwnershipTransferModel
{
    public Guid PreviousOwnerMembershipId { get; set; }
    public Guid NewOwnerMembershipId { get; set; }
}

public class OwnershipTransferCandidate
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
    public bool IsEffective { get; set; }
}

/// <summary>
/// Result of listing pending access requests.
/// </summary>
public class PendingAccessRequestListResult
{
    public IReadOnlyList<PendingAccessRequestItem> Requests { get; set; } = Array.Empty<PendingAccessRequestItem>();
}

/// <summary>
/// Single pending access request item.
/// </summary>
public class PendingAccessRequestItem
{
    public Guid RequestId { get; set; }
    public Guid AppUserId { get; set; }
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public string RequestedRoleCode { get; set; } = default!;
    public string RequestedRoleLabel { get; set; } = default!;
    public string? Message { get; set; }
    public DateTime RequestedAt { get; set; }
}

