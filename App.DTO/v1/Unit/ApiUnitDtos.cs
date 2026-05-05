using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Unit;

public class UnitDashboardResponseDto
{
    public ApiDashboardDto Dashboard { get; set; } = new();
}

public class UnitProfileResponseDto
{
    public UnitProfileDto Profile { get; set; } = new();
}

public class UnitProfileDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public string? Notes { get; set; }
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class UpdateUnitProfileRequestDto
{
    [Required]
    [MaxLength(64)]
    public string UnitNr { get; set; } = string.Empty;

    public int? FloorNr { get; set; }

    public decimal? SizeM2 { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
    
}

public class DeleteUnitProfileRequestDto
{
    [Required]
    [MaxLength(64)]
    public string ConfirmationUnitNr { get; set; } = string.Empty;
}
