using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.VendorContacts;

public class VendorContactIndexViewModel : IAppChromePage
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

    public VendorContactAttachExistingFormViewModel ExistingForm { get; set; } = new();
    public VendorContactCreateFormViewModel NewForm { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<VendorContactAssignmentViewModel> Contacts { get; set; } =
        Array.Empty<VendorContactAssignmentViewModel>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ExistingContactOptions { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ContactTypeOptions { get; set; } = Array.Empty<SelectListItem>();
}

public class VendorContactEditViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }
    public Guid VendorContactId { get; set; }
    public Guid ContactId { get; set; }

    [ValidateNever]
    public string VendorName { get; set; } = default!;

    [ValidateNever]
    public string ContactLabel { get; set; } = default!;

    public VendorContactMetadataFormViewModel Form { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ExistingContactOptions { get; set; } = Array.Empty<SelectListItem>();
}

public class VendorContactDeleteViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }
    public Guid VendorContactId { get; set; }

    [ValidateNever]
    public string VendorName { get; set; } = default!;

    [ValidateNever]
    public string ContactLabel { get; set; } = default!;
}

public class VendorContactAttachExistingFormViewModel : VendorContactMetadataFormViewModel
{
    [Display(Name = "Contact", ResourceType = typeof(UiText))]
    public Guid ContactId { get; set; }
}

public class VendorContactCreateFormViewModel : VendorContactMetadataFormViewModel
{
    [Display(Name = "Contact", ResourceType = typeof(UiText))]
    public Guid ContactTypeId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Contact", ResourceType = typeof(UiText))]
    public string ContactValue { get; set; } = default!;

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? ContactNotes { get; set; }
}

public class VendorContactMetadataFormViewModel
{
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "FullName", ResourceType = typeof(UiText))]
    public string? FullName { get; set; }

    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Role", ResourceType = typeof(UiText))]
    public string? RoleTitle { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "ValidFrom", ResourceType = typeof(UiText))]
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    [Display(Name = "ValidTo", ResourceType = typeof(UiText))]
    public DateOnly? ValidTo { get; set; }

    [Display(Name = "Active", ResourceType = typeof(UiText))]
    public bool Confirmed { get; set; } = true;

    [Display(Name = "Active", ResourceType = typeof(UiText))]
    public bool IsPrimary { get; set; }
}

public class VendorContactAssignmentViewModel
{
    public Guid VendorContactId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactTypeLabel { get; set; } = default!;
    public string ContactValue { get; set; } = default!;
    public string? FullName { get; set; }
    public string? RoleTitle { get; set; }
    public bool IsPrimary { get; set; }
    public bool Confirmed { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
