using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.ManagementResidents;

public class ManagementResidentsPageViewModel : IHasPageShell<ManagementPageShellViewModel>
{
    [ValidateNever]
    public ManagementPageShellViewModel PageShell { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    [ValidateNever]
    public IReadOnlyList<ManagementResidentListItemViewModel> Residents { get; set; } = Array.Empty<ManagementResidentListItemViewModel>();

    public AddManagementResidentViewModel AddResident { get; set; } = new();
}

public class ManagementResidentListItemViewModel
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public string? PreferredLanguage { get; set; }
    public bool IsActive { get; set; }
}

public class AddManagementResidentViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(100, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "FirstName", ResourceType = typeof(UiText))]
    public string FirstName { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(100, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "LastName", ResourceType = typeof(UiText))]
    public string LastName { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(50, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "IdCode", ResourceType = typeof(UiText))]
    public string IdCode { get; set; } = default!;

    [StringLength(10, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "PreferredLanguage", ResourceType = typeof(UiText))]
    public string? PreferredLanguage { get; set; }
}
