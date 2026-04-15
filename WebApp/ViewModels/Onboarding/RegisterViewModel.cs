using System.ComponentModel.DataAnnotations;
using App.Resources.Views;

namespace WebApp.ViewModels.Onboarding;

public class RegisterViewModel
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
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.Password), ResourceType = typeof(UiText))]
    public string Password { get; set; } = default!;

    [Required(
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(
        100,
        MinimumLength = 1,
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.FirstName), ResourceType = typeof(UiText))]
    public string FirstName { get; set; } = default!;

    [Required(
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(
        100,
        MinimumLength = 1,
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.LastName), ResourceType = typeof(UiText))]
    public string LastName { get; set; } = default!;
}

