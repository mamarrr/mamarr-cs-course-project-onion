using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Management;

public class ManagementResidentsResponseDto
{
    public IReadOnlyList<ManagementResidentSummaryDto> Residents { get; set; } = Array.Empty<ManagementResidentSummaryDto>();
}

public class ManagementResidentSummaryDto
{
    public Guid ResidentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class CreateManagementResidentRequestDto
{
    [Required]
    [MaxLength(128)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string IdCode { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? PreferredLanguage { get; set; }
}

public class CreateManagementResidentResponseDto
{
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public ApiRouteContextDto RouteContext { get; set; } = new();
}
