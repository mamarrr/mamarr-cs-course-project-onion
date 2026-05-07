using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.ResidentContacts;

public class ResidentContactIndexViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    [ValidateNever]
    public string ResidentIdCode { get; set; } = default!;

    [ValidateNever]
    public string ResidentName { get; set; } = default!;

    public ResidentContactAttachExistingFormViewModel ExistingForm { get; set; } = new();
    public ResidentContactCreateFormViewModel NewForm { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<ResidentContactAssignmentViewModel> Contacts { get; set; } =
        Array.Empty<ResidentContactAssignmentViewModel>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ExistingContactOptions { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> ContactTypeOptions { get; set; } = Array.Empty<SelectListItem>();
}

public class ResidentContactEditViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    [ValidateNever]
    public string ResidentIdCode { get; set; } = default!;

    public Guid ResidentContactId { get; set; }
    public Guid ContactId { get; set; }

    [ValidateNever]
    public string ResidentName { get; set; } = default!;

    [ValidateNever]
    public string ContactLabel { get; set; } = default!;

    public ResidentContactMetadataFormViewModel Form { get; set; } = new();
}

public class ResidentContactDeleteViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    [ValidateNever]
    public string ResidentIdCode { get; set; } = default!;

    public Guid ResidentContactId { get; set; }

    [ValidateNever]
    public string ResidentName { get; set; } = default!;

    [ValidateNever]
    public string ContactLabel { get; set; } = default!;
}

public class ResidentContactAttachExistingFormViewModel : ResidentContactMetadataFormViewModel
{
    [Display(Name = "Contact", ResourceType = typeof(UiText))]
    public Guid ContactId { get; set; }
}

public class ResidentContactCreateFormViewModel : ResidentContactMetadataFormViewModel
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

public class ResidentContactMetadataFormViewModel
{
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

public class ResidentContactAssignmentViewModel
{
    public Guid ResidentContactId { get; set; }
    public Guid ContactId { get; set; }
    public string ContactTypeLabel { get; set; } = default!;
    public string ContactValue { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public bool Confirmed { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}
