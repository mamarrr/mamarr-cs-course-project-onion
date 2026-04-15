namespace App.BLL.ManagementCustomers;

public class ManagementCustomersAuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementCustomersAuthorizedContext? Context { get; set; }
}

public class ManagementCustomersAuthorizedContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
}

public class ManagementCustomerListResult
{
    public IReadOnlyList<ManagementCustomerListItem> Customers { get; set; } = Array.Empty<ManagementCustomerListItem>();
}

public class ManagementCustomerListItem
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
}

public class ManagementCustomerDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public bool CustomerNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementCustomerDashboardContext? Context { get; set; }
}

public class ManagementCustomerDashboardContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
}

public class ManagementCustomerCreateRequest
{
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
}

public class ManagementCustomerCreateResult
{
    public bool Success { get; set; }
    public bool DuplicateRegistryCode { get; set; }
    public bool InvalidBillingEmail { get; set; }
    public Guid? CreatedCustomerId { get; set; }
    public string? ErrorMessage { get; set; }
}
