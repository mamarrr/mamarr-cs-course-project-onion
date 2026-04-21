namespace App.BLL.ResidentWorkspace.Residents;

public class CompanyResidentsAuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public CompanyResidentsAuthorizedContext? Context { get; set; }
}

public class CompanyResidentsAuthorizedContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
}

public class CompanyResidentListResult
{
    public IReadOnlyList<CompanyResidentListItem> Residents { get; set; } = Array.Empty<CompanyResidentListItem>();
}

public class CompanyResidentListItem
{
    public Guid ResidentId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
}

public class ResidentCreateRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
}

public class ResidentCreateResult
{
    public bool Success { get; set; }
    public bool DuplicateIdCode { get; set; }
    public bool InvalidFirstName { get; set; }
    public bool InvalidLastName { get; set; }
    public bool InvalidIdCode { get; set; }
    public Guid? CreatedResidentId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ResidentDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public bool ResidentNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ResidentDashboardContext? Context { get; set; }
}

public class ResidentDashboardContext
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
