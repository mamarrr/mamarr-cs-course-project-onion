using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Resident;

public class ResidentDashboardResponseDto
{
    public ApiDashboardDto Dashboard { get; set; } = new();
}

public class ResidentProfileResponseDto
{
    public ResidentProfileDto Profile { get; set; } = new();
}

public class ResidentProfileDto
{
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class UpdateResidentProfileRequestDto
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

public class DeleteResidentProfileRequestDto
{
    [Required]
    [MaxLength(64)]
    public string ConfirmationIdCode { get; set; } = string.Empty;
}
