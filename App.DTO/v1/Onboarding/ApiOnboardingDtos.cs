using System.ComponentModel.DataAnnotations;
using App.DTO.v1.Shared;

namespace App.DTO.v1.Onboarding;

public class OnboardingContextSummaryDto
{
    public string ContextType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class OnboardingContextsResponseDto
{
    public IReadOnlyList<OnboardingContextSummaryDto> Contexts { get; set; } = Array.Empty<OnboardingContextSummaryDto>();
    public OnboardingContextSummaryDto? DefaultContext { get; set; }
}

public class CreateManagementCompanyRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string RegistryCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string VatNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(64)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
}

public class CreateManagementCompanyResponseDto
{
    public Guid ManagementCompanyId { get; set; }
    public string ManagementCompanySlug { get; set; } = string.Empty;
    public ApiRouteContextDto RouteContext { get; set; } = new();
}

public class SpaJwtResponseDto : Identity.JWTResponse
{
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public OnboardingContextsResponseDto? Onboarding { get; set; }
}
