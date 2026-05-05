using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Resident;

public class ResidentUnitsBootstrapResponseDto
{
    public ApiRouteContextDto RouteContext { get; set; } = new();
    public IReadOnlyList<ResidentUnitLeaseDto> Leases { get; set; } = Array.Empty<ResidentUnitLeaseDto>();
    public IReadOnlyList<LookupOptionDto> LeaseRoles { get; set; } = Array.Empty<LookupOptionDto>();
}

public class ResidentUnitLeaseDto
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public string UnitSlug { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = string.Empty;
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class ResidentPropertySearchResponseDto
{
    public IReadOnlyList<ResidentPropertySearchResultDto> Properties { get; set; } = Array.Empty<ResidentPropertySearchResultDto>();
}

public class ResidentPropertySearchResultDto
{
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class ResidentPropertyUnitsResponseDto
{
    public IReadOnlyList<ResidentPropertyUnitOptionDto> Units { get; set; } = Array.Empty<ResidentPropertyUnitOptionDto>();
}

public class ResidentPropertyUnitOptionDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
}

public class CreateResidentLeaseRequestDto
{
    [Required]
    public Guid? UnitId { get; set; }

    [Required]
    public Guid? LeaseRoleId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
    

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateResidentLeaseRequestDto
{
    [Required]
    public Guid? LeaseRoleId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
    

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class ResidentLeaseCommandResponseDto
{
    public Guid LeaseId { get; set; }
}
