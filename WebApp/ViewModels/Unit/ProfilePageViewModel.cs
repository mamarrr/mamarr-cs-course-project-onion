using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Unit;

public class ProfilePageViewModel : IAppChromePage
{
    public AppChromeViewModel AppChrome { get; init; } = new();
    public string CompanySlug { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PropertySlug { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string UnitSlug { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public string? SuccessMessage { get; init; }
    public UnitProfileEditViewModel Edit { get; init; } = new();
}

public class UnitProfileEditViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = nameof(UiText.UnitNr), ResourceType = typeof(UiText))]
    public string UnitNr { get; set; } = string.Empty;

    [Display(Name = nameof(UiText.FloorNr), ResourceType = typeof(UiText))]
    public int? FloorNr { get; set; }

    [Display(Name = nameof(UiText.SizeM2), ResourceType = typeof(UiText))]
    public decimal? SizeM2 { get; set; }

    [Display(Name = nameof(UiText.Notes), ResourceType = typeof(UiText))]
    public string? Notes { get; set; }

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; }

    [Display(Name = nameof(UiText.DeleteConfirmation), ResourceType = typeof(UiText))]
    public string? DeleteConfirmation { get; set; }
}
