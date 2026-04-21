namespace App.BLL.Onboarding.CompanyJoinRequests;

public class CreateManagementCompanyJoinRequest
{
    public Guid AppUserId { get; set; }
    public string RegistryCode { get; set; } = default!;
    public Guid RequestedRoleId { get; set; }
    public string? Message { get; set; }
}

public class CreateManagementCompanyJoinRequestResult
{
    public bool Success { get; set; }
    public bool UnknownRegistryCode { get; set; }
    public bool InvalidRole { get; set; }
    public bool DuplicatePendingRequest { get; set; }
    public bool AlreadyMember { get; set; }
    public Guid? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManagementCompanyJoinRequestListItem
{
    public Guid RequestId { get; set; }
    public Guid AppUserId { get; set; }
    public string RequesterName { get; set; } = default!;
    public string RequesterEmail { get; set; } = default!;
    public Guid RequestedRoleId { get; set; }
    public string RequestedRoleCode { get; set; } = default!;
    public string RequestedRoleLabel { get; set; } = default!;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResolveManagementCompanyJoinRequestResult
{
    public bool Success { get; set; }
    public bool NotFound { get; set; }
    public bool Forbidden { get; set; }
    public bool AlreadyResolved { get; set; }
    public bool AlreadyMember { get; set; }
    public string? ErrorMessage { get; set; }
}

