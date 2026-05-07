namespace App.BLL.DTO.ManagementCompanies.Models;

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
