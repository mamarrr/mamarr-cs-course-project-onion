using App.Domain;
using App.Domain.Identity;

namespace App.BLL.ManagementUsers;

/// <summary>
/// Result of authorization check for management user administration.
/// </summary>
public class ManagementUserAdminAuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public ManagementUserAdminAuthorizedContext? Context { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Authorized context for performing management user administration operations.
/// </summary>
public class ManagementUserAdminAuthorizedContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid ActorMembershipId { get; set; }
    public string ActorRoleCode { get; set; } = default!;
}

/// <summary>
/// Result of listing company members.
/// </summary>
public class ManagementUserListResult
{
    public IReadOnlyList<ManagementUserListItem> Members { get; set; } = Array.Empty<ManagementUserListItem>();
}

/// <summary>
/// Single member item in the company users list.
/// </summary>
public class ManagementUserListItem
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = default!;
    public string RoleLabel { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActor { get; set; }
}

/// <summary>
/// Result of getting a membership for editing.
/// </summary>
public class ManagementUserEditResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public ManagementUserEditModel? Data { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Model for editing a membership.
/// </summary>
public class ManagementUserEditModel
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

/// <summary>
/// Request to add a user by email.
/// </summary>
public class ManagementUserAddRequest
{
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Result of adding a user by email.
/// </summary>
public class ManagementUserAddResult
{
    public bool Success { get; set; }
    public bool UserNotFound { get; set; }
    public bool DuplicateMembership { get; set; }
    public bool InvalidRole { get; set; }
    public Guid? CreatedMembershipId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request to update a membership.
/// </summary>
public class ManagementUserUpdateRequest
{
    public Guid RoleId { get; set; }
    public string JobTitle { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

/// <summary>
/// Result of updating a membership.
/// </summary>
public class ManagementUserUpdateResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public bool InvalidRole { get; set; }
    public bool WouldLeaveCompanyWithoutManager { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of deleting a membership.
/// </summary>
public class ManagementUserDeleteResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public bool WouldLeaveCompanyWithoutManager { get; set; }
    public string? ErrorMessage { get; set; }
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
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public string RequestedRoleCode { get; set; } = default!;
    public string RequestedRoleLabel { get; set; } = default!;
    public DateTime RequestedAt { get; set; }
}
