using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Customer;

public class CustomerDashboardResponseDto
{
    public ApiDashboardDto Dashboard { get; set; } = new();
}

public class CustomerPropertiesResponseDto
{
    public IReadOnlyList<CustomerPropertySummaryDto> Properties { get; set; } = Array.Empty<CustomerPropertySummaryDto>();
    public IReadOnlyList<LookupOptionDto> PropertyTypeOptions { get; set; } = Array.Empty<LookupOptionDto>();
}

public class CustomerPropertySummaryDto
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public Guid PropertyTypeId { get; set; }
    public string PropertyTypeCode { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class CustomerProfileResponseDto
{
    public CustomerProfileDto Profile { get; set; } = new();
}

public class CustomerProfileDto
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }
    public string? Phone { get; set; }
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class UpdateCustomerProfileRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string RegistryCode { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(255)]
    public string? BillingEmail { get; set; }

    [MaxLength(255)]
    public string? BillingAddress { get; set; }

    [MaxLength(64)]
    public string? Phone { get; set; }
    
}

public class DeleteCustomerProfileRequestDto
{
    [Required]
    [MaxLength(255)]
    public string ConfirmationName { get; set; } = string.Empty;
}
