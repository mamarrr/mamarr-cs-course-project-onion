using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Resident;

public class ProfilePageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string ResidentDisplayName { get; init; } = string.Empty;
    public string ResidentIdCode { get; init; } = string.Empty;
    public string? SuccessMessage { get; init; }
    public ResidentProfileEditViewModel Edit { get; init; } = new();
}

public class ResidentProfileEditViewModel
{
    
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.FirstName), ResourceType = typeof(UiText))]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.LastName), ResourceType = typeof(UiText))]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.IdCode), ResourceType = typeof(UiText))]
    public string IdCode { get; set; } = default!;
    
    [Display(Name = nameof(UiText.PreferredLanguage), ResourceType = typeof(UiText))]
    public string? PreferredLanguage { get; set; }
    
    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; }
    
    [Display(Name = nameof(UiText.DeleteConfirmation), ResourceType = typeof(UiText))]
    public string? DeleteConfirmation { get; set; }
}
