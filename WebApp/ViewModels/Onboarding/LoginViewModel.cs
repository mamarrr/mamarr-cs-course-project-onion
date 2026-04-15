using System.ComponentModel.DataAnnotations;
using App.Resources.Views;

namespace WebApp.ViewModels.Onboarding;

public class LoginViewModel
{
    [Required(
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [EmailAddress(
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.InvalidEmailAddress))]
    [Display(Name = nameof(UiText.Email), ResourceType = typeof(UiText))]
    public string Email { get; set; } = default!;

    [Required(
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [DataType(DataType.Password)]
    [Display(Name = nameof(UiText.Password), ResourceType = typeof(UiText))]
    public string Password { get; set; } = default!;

    [Display(Name = nameof(UiText.RememberMe), ResourceType = typeof(UiText))]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

