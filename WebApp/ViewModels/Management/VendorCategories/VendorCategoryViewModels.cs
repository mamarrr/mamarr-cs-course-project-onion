using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.VendorCategories;

public class VendorCategoryIndexViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }

    [ValidateNever]
    public string VendorName { get; set; } = default!;

    public VendorCategoryFormViewModel Form { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<VendorCategoryAssignmentViewModel> Assignments { get; set; } =
        Array.Empty<VendorCategoryAssignmentViewModel>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> AvailableCategories { get; set; } = Array.Empty<SelectListItem>();
}

public class VendorCategoryEditViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }

    [ValidateNever]
    public string VendorName { get; set; } = default!;

    public Guid TicketCategoryId { get; set; }

    [ValidateNever]
    public string CategoryLabel { get; set; } = default!;
    public VendorCategoryNotesFormViewModel Form { get; set; } = new();
}

public class VendorCategoryFormViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "TicketCategory", ResourceType = typeof(UiText))]
    public Guid TicketCategoryId { get; set; }

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}

public class VendorCategoryNotesFormViewModel
{
    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}

public class VendorCategoryAssignmentViewModel
{
    public Guid TicketCategoryId { get; set; }
    public string CategoryCode { get; set; } = default!;
    public string CategoryLabel { get; set; } = default!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
