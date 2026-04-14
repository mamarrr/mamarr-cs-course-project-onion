using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Onboarding;

public class CreateManagementCompanyViewModel
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string RegistryCode { get; set; } = default!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string VatNumber { get; set; } = default!;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Phone { get; set; } = default!;

    [Required]
    [StringLength(300, MinimumLength = 1)]
    public string Address { get; set; } = default!;
}
