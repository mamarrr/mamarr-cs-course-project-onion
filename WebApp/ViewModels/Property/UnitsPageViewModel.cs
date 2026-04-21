using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Property;

public class UnitsPageViewModel : IHasPageShell<PropertyPageShellViewModel>
{
    [ValidateNever]
    public PropertyPageShellViewModel PageShell { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = string.Empty;

    [ValidateNever]
    public string CompanyName { get; set; } = string.Empty;

    [ValidateNever]
    public string CustomerSlug { get; set; } = string.Empty;

    [ValidateNever]
    public string CustomerName { get; set; } = string.Empty;

    [ValidateNever]
    public string PropertySlug { get; set; } = string.Empty;

    [ValidateNever]
    public string PropertyName { get; set; } = string.Empty;

    [ValidateNever]
    public IReadOnlyList<PropertyUnitListItemViewModel> Units { get; set; } = Array.Empty<PropertyUnitListItemViewModel>();

    public AddUnitViewModel AddUnit { get; set; } = new();
}

public class PropertyUnitListItemViewModel
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
}

public class AddUnitViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(100, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "UnitNr")]
    public string UnitNr { get; set; } = string.Empty;

    [Range(-200, 300, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidData))]
    [Display(Name = "FloorNr")]
    public int? FloorNr { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessageResourceType = typeof(UiText),
        ErrorMessageResourceName = nameof(UiText.InvalidData))]
    [Display(Name = "SizeM2")]
    public decimal? SizeM2 { get; set; }

    [StringLength(2000, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.Notes), ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}
