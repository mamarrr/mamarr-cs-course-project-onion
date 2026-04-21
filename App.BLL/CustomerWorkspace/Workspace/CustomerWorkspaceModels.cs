namespace App.BLL.CustomerWorkspace.Workspace;

public class CustomerWorkspaceAuthorizationResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public CustomerWorkspaceAuthorizedContext? Context { get; set; }
}

public class CustomerWorkspaceAuthorizedContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
}

public class CompanyCustomerListResult
{
    public IReadOnlyList<CompanyCustomerListItem> Customers { get; set; } = Array.Empty<CompanyCustomerListItem>();
}

public class CompanyCustomerListItem
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
}

public class CustomerWorkspaceDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool IsForbidden { get; set; }
    public bool CompanyNotFound { get; set; }
    public bool CustomerNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public CustomerWorkspaceDashboardContext? Context { get; set; }
}

public class CustomerWorkspaceDashboardContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
}

public class CustomerCreateRequest
{
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
}

public class CustomerCreateResult
{
    public bool Success { get; set; }
    public bool DuplicateRegistryCode { get; set; }
    public bool InvalidBillingEmail { get; set; }
    public Guid? CreatedCustomerId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CustomerPropertyListResult
{
    public IReadOnlyList<CustomerPropertyListItem> Properties { get; set; } = Array.Empty<CustomerPropertyListItem>();
}

public class CustomerPropertyListItem
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

public class PropertyCreateRequest
{
    public string Name { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public Guid PropertyTypeId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PropertyCreateResult
{
    public bool Success { get; set; }
    public bool InvalidPropertyType { get; set; }
    public Guid? CreatedPropertyId { get; set; }
    public string? CreatedPropertySlug { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PropertyDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool PropertyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public PropertyDashboardContext? Context { get; set; }
}

public class PropertyDashboardContext
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
