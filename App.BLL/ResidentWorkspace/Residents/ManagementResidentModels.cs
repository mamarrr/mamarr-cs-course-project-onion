namespace App.BLL.ResidentWorkspace.Residents;

public class ManagementResidentsAuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementResidentsAuthorizedContext? Context { get; set; }
}

public class ManagementResidentsAuthorizedContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
}

public class ManagementResidentListResult
{
    public IReadOnlyList<ManagementResidentListItem> Residents { get; set; } = Array.Empty<ManagementResidentListItem>();
}

public class ManagementResidentListItem
{
    public Guid ResidentId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
}

public class ManagementResidentCreateRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
}

public class ManagementResidentCreateResult
{
    public bool Success { get; set; }
    public bool DuplicateIdCode { get; set; }
    public bool InvalidFirstName { get; set; }
    public bool InvalidLastName { get; set; }
    public bool InvalidIdCode { get; set; }
    public Guid? CreatedResidentId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManagementResidentDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public bool ResidentNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementResidentDashboardContext? Context { get; set; }
}

public class ManagementResidentDashboardContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
}
