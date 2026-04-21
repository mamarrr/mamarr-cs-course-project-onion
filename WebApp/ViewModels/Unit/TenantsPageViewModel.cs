using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.ViewModels.Unit;

public class TenantsPageViewModel : IHasPageShell<UnitPageShellViewModel>
{
    [ValidateNever]
    public UnitPageShellViewModel PageShell { get; init; } = new();

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
    public string UnitSlug { get; set; } = string.Empty;

    [ValidateNever]
    public string UnitName { get; set; } = string.Empty;

    [ValidateNever]
    public string? SuccessMessage { get; set; }

    [ValidateNever]
    public string? ErrorMessage { get; set; }

    [ValidateNever]
    public Guid? ActiveEditLeaseId { get; set; }

    [ValidateNever]
    public IReadOnlyList<UnitTenantLeaseListItemViewModel> Leases { get; set; } = Array.Empty<UnitTenantLeaseListItemViewModel>();

    [ValidateNever]
    public IReadOnlyList<UnitLeaseRoleOptionViewModel> LeaseRoleOptions { get; set; } = Array.Empty<UnitLeaseRoleOptionViewModel>();

    [ValidateNever]
    public IReadOnlyList<UnitLeaseResidentSearchResultViewModel> ResidentSearchResults { get; set; } = Array.Empty<UnitLeaseResidentSearchResultViewModel>();

    public AddUnitLeaseViewModel AddLease { get; set; } = new();

    public EditUnitLeaseViewModel EditLease { get; set; } = new();
}

public class UnitTenantLeaseListItemViewModel
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public string ResidentFullName { get; set; } = string.Empty;
    public string ResidentIdCode { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UnitLeaseRoleOptionViewModel
{
    public Guid LeaseRoleId { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class UnitLeaseResidentSearchResultViewModel
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AddUnitLeaseViewModel
{
    [StringLength(200, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Resident", ResourceType = typeof(UiText))]
    public string? ResidentSearchTerm { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "Resident", ResourceType = typeof(UiText))]
    public Guid? ResidentId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "LeaseRole", ResourceType = typeof(UiText))]
    public Guid? LeaseRoleId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [DataType(DataType.Date)]
    [Display(Name = "StartDate", ResourceType = typeof(UiText))]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "EndDate", ResourceType = typeof(UiText))]
    public DateTime? EndDate { get; set; }

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; } = true;

    [StringLength(2000, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.Notes), ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}

public class EditUnitLeaseViewModel
{
    [ValidateNever]
    public Guid LeaseId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "LeaseRole", ResourceType = typeof(UiText))]
    public Guid? LeaseRoleId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [DataType(DataType.Date)]
    [Display(Name = "StartDate", ResourceType = typeof(UiText))]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "EndDate", ResourceType = typeof(UiText))]
    public DateTime? EndDate { get; set; }

    [Display(Name = nameof(UiText.Active), ResourceType = typeof(UiText))]
    public bool IsActive { get; set; }

    [StringLength(2000, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = nameof(UiText.Notes), ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}
