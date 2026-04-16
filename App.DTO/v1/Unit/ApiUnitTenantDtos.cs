using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Unit;

public class UnitTenantsBootstrapResponseDto
{
    public ApiRouteContextDto RouteContext { get; set; } = new();
    public IReadOnlyList<UnitTenantLeaseDto> Leases { get; set; } = Array.Empty<UnitTenantLeaseDto>();
    public IReadOnlyList<LookupOptionDto> LeaseRoles { get; set; } = Array.Empty<LookupOptionDto>();
}

public class UnitTenantLeaseDto
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string ResidentFullName { get; set; } = string.Empty;
    public string ResidentIdCode { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = string.Empty;
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UnitResidentSearchResponseDto
{
    public IReadOnlyList<UnitResidentSearchResultDto> Residents { get; set; } = Array.Empty<UnitResidentSearchResultDto>();
}

public class UnitResidentSearchResultDto
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUnitLeaseRequestDto
{
    [Required]
    public Guid? ResidentId { get; set; }

    [Required]
    public Guid? LeaseRoleId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateUnitLeaseRequestDto
{
    [Required]
    public Guid? LeaseRoleId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UnitLeaseCommandResponseDto
{
    public Guid LeaseId { get; set; }
}
