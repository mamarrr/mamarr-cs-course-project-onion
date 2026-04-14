using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Onboarding;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = default!;
}

