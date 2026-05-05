using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Customer.CustomerProfile;

public class ProfilePageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? SuccessMessage { get; init; }
    public CustomerProfileEditViewModel Edit { get; init; } = new();
}

public class CustomerProfileEditViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Name), ResourceType = typeof(UiText))]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.RegistryCode), ResourceType = typeof(UiText))]
    public string RegistryCode { get; set; } = string.Empty;

    [EmailAddress(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidEmailAddress))]
    [Display(Name = nameof(UiText.BillingEmail), ResourceType = typeof(UiText))]
    public string? BillingEmail { get; set; }

    [Display(Name = nameof(UiText.BillingAddress), ResourceType = typeof(UiText))]
    public string? BillingAddress { get; set; }

    [Display(Name = nameof(UiText.Phone), ResourceType = typeof(UiText))]
    public string? Phone { get; set; }

    [Display(Name = nameof(UiText.DeleteConfirmation), ResourceType = typeof(UiText))]
    public string? DeleteConfirmation { get; set; }
}
