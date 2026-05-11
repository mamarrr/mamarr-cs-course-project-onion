namespace App.DTO.v1.Portal.Users;

public class CompanyUserListItemDto
{
    public Guid MembershipId { get; set; }
    public Guid AppUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
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
    public string ProtectedReasonCode { get; set; } = string.Empty;
}
