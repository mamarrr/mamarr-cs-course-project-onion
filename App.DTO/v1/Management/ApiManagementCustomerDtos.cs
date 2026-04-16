using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Management;

public class ManagementCustomersResponseDto
{
    public IReadOnlyList<ManagementCustomerSummaryDto> Customers { get; set; } = Array.Empty<ManagementCustomerSummaryDto>();
}

public class ManagementCustomerSummaryDto
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

public class CreateManagementCustomerRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string RegistryCode { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(320)]
    public string? BillingEmail { get; set; }

    [MaxLength(500)]
    public string? BillingAddress { get; set; }

    [Phone]
    [MaxLength(64)]
    public string? Phone { get; set; }
}

public class CreateManagementCustomerResponseDto
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public ApiRouteContextDto RouteContext { get; set; } = new();
}
