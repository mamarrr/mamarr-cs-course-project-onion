using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Management.Profile;

public class ManagementCompanyProfilePageViewModel : IHasPageShell<ManagementPageShellViewModel>
{
    public ManagementPageShellViewModel PageShell { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string? SuccessMessage { get; init; }
    public ManagementCompanyProfileEditViewModel Edit { get; init; } = new();
}

public class ManagementCompanyProfileEditViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Name), ResourceType = typeof(UiText))]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.RegistryCode), ResourceType = typeof(UiText))]
    public string RegistryCode { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.VatNumber), ResourceType = typeof(UiText))]
    public string VatNumber { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [EmailAddress(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidEmailAddress))]
    [Display(Name = nameof(UiText.Email), ResourceType = typeof(UiText))]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Phone), ResourceType = typeof(UiText))]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Address), ResourceType = typeof(UiText))]
    public string Address { get; set; } = string.Empty;

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; }

    [Display(Name = nameof(UiText.DeleteConfirmation), ResourceType = typeof(UiText))]
    public string? DeleteConfirmation { get; set; }
}
