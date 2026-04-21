namespace App.BLL.CustomerWorkspace.Workspace;

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

public class ManagementCustomerPropertyListResult
{
    public IReadOnlyList<ManagementCustomerPropertyListItem> Properties { get; set; } = Array.Empty<ManagementCustomerPropertyListItem>();
}

public class ManagementCustomerPropertyListItem
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public Guid PropertyTypeId { get; set; }
    public string PropertyTypeCode { get; set; } = default!;
    public string PropertyTypeLabel { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ManagementCustomerPropertyCreateRequest
{
    public string Name { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public Guid PropertyTypeId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ManagementCustomerPropertyCreateResult
{
    public bool Success { get; set; }
    public bool InvalidPropertyType { get; set; }
    public Guid? CreatedPropertyId { get; set; }
    public string? CreatedPropertySlug { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManagementCustomerPropertyDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool PropertyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementCustomerPropertyDashboardContext? Context { get; set; }
}

public class ManagementCustomerPropertyDashboardContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
}
