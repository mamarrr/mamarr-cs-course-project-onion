using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Property;

public class CreateCustomerPropertyRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    public Guid? PropertyTypeId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
    
}

public class CreateCustomerPropertyResponseDto
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class PropertyDashboardResponseDto
{
    public ApiDashboardDto Dashboard { get; set; } = new();
}

public class PropertyUnitsResponseDto
{
    public IReadOnlyList<PropertyUnitSummaryDto> Units { get; set; } = Array.Empty<PropertyUnitSummaryDto>();
}

public class PropertyUnitSummaryDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class CreatePropertyUnitRequestDto
{
    [Required]
    [MaxLength(64)]
    public string UnitNr { get; set; } = string.Empty;

    public int? FloorNr { get; set; }

    public decimal? SizeM2 { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class CreatePropertyUnitResponseDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class PropertyProfileResponseDto
{
    public PropertyProfileDto Profile { get; set; } = new();
}

public class PropertyProfileDto
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class UpdatePropertyProfileRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PostalCode { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }
    
}

public class DeletePropertyProfileRequestDto
{
    [Required]
    [MaxLength(255)]
    public string ConfirmationName { get; set; } = string.Empty;
}
