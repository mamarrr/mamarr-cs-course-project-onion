using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Property;

public class ProfilePageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PropertySlug { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string? SuccessMessage { get; init; }
    public PropertyProfileEditViewModel Edit { get; init; } = new();
}

public class PropertyProfileEditViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.Name), ResourceType = typeof(UiText))]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.AddressLine), ResourceType = typeof(UiText))]
    public string AddressLine { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.City), ResourceType = typeof(UiText))]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.PostalCode), ResourceType = typeof(UiText))]
    public string PostalCode { get; set; } = string.Empty;

    [Display(Name = nameof(UiText.Notes), ResourceType = typeof(UiText))]
    public string? Notes { get; set; }

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; }

    [Display(Name = nameof(UiText.DeleteConfirmation), ResourceType = typeof(UiText))]
    public string? DeleteConfirmation { get; set; }
}
