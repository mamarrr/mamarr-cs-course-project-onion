using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Resident;

public class UnitsPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = string.Empty;

    [ValidateNever]
    public string CompanyName { get; set; } = string.Empty;

    [ValidateNever]
    public string ResidentIdCode { get; set; } = string.Empty;

    [ValidateNever]
    public string ResidentDisplayName { get; set; } = string.Empty;

    [ValidateNever]
    public string? ResidentSupportingText { get; set; }

    [ValidateNever]
    public string? SuccessMessage { get; set; }

    [ValidateNever]
    public string? ErrorMessage { get; set; }

    [ValidateNever]
    public Guid? ActiveEditLeaseId { get; set; }

    [ValidateNever]
    public IReadOnlyList<ResidentLeaseListItemViewModel> Leases { get; set; } = Array.Empty<ResidentLeaseListItemViewModel>();

    [ValidateNever]
    public IReadOnlyList<ResidentLeaseRoleOptionViewModel> LeaseRoleOptions { get; set; } = Array.Empty<ResidentLeaseRoleOptionViewModel>();

    [ValidateNever]
    public IReadOnlyList<ResidentLeasePropertySearchResultViewModel> PropertySearchResults { get; set; } = Array.Empty<ResidentLeasePropertySearchResultViewModel>();

    [ValidateNever]
    public IReadOnlyList<ResidentLeaseUnitOptionViewModel> UnitOptions { get; set; } = Array.Empty<ResidentLeaseUnitOptionViewModel>();

    public AddResidentLeaseViewModel AddLease { get; set; } = new();

    public EditResidentLeaseViewModel EditLease { get; set; } = new();
}

public class ResidentLeaseListItemViewModel
{
    public Guid LeaseId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public string UnitNr { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class ResidentLeaseRoleOptionViewModel
{
    public Guid LeaseRoleId { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class ResidentLeasePropertySearchResultViewModel
{
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class ResidentLeaseUnitOptionViewModel
{
    public Guid UnitId { get; set; }
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public bool IsActive { get; set; }
}

public class AddResidentLeaseViewModel
{
    [StringLength(200, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Property", ResourceType = typeof(UiText))]
    public string? PropertySearchTerm { get; set; }

    [Display(Name = "Property", ResourceType = typeof(UiText))]
    public Guid? PropertyId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "Unit", ResourceType = typeof(UiText))]
    public Guid? UnitId { get; set; }

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

public class EditResidentLeaseViewModel
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
