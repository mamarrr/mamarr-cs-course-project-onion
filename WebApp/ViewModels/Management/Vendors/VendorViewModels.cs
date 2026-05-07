using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.Vendors;

public class VendorsPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public VendorFilterViewModel Filter { get; set; } = new();
    public VendorFormViewModel Form { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<VendorListItemViewModel> Vendors { get; set; } = Array.Empty<VendorListItemViewModel>();

    [ValidateNever]
    public VendorOptionsViewModel Options { get; set; } = new();
}

public class VendorIndexViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    [ValidateNever]
    public IReadOnlyList<VendorListItemViewModel> Vendors { get; set; } = Array.Empty<VendorListItemViewModel>();
}

public class VendorFilterViewModel
{
    [Display(Name = "Search", ResourceType = typeof(UiText))]
    public string? Search { get; set; }

    [Display(Name = "Active", ResourceType = typeof(UiText))]
    public bool IncludeInactive { get; set; }

    [Display(Name = "TicketCategory", ResourceType = typeof(UiText))]
    public Guid? TicketCategoryId { get; set; }
}

public class VendorListItemViewModel
{
    public Guid VendorId { get; set; }
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public int ActiveCategoryCount { get; set; }
    public int AssignedTicketCount { get; set; }
    public int ContactCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VendorDetailsPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string Notes { get; set; } = default!;
    public string? TicketSearch { get; set; }
    public VendorFormViewModel Form { get; set; } = new();
    public AddVendorCategoryViewModel CategoryForm { get; set; } = new();
    public AddVendorContactViewModel ContactForm { get; set; } = new();
    public AssignVendorTicketViewModel AssignTicketForm { get; set; } = new();
    public AddVendorScheduledWorkViewModel ScheduledWorkForm { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<VendorCategoryViewModel> Categories { get; set; } = Array.Empty<VendorCategoryViewModel>();

    [ValidateNever]
    public IReadOnlyList<VendorTicketViewModel> Tickets { get; set; } = Array.Empty<VendorTicketViewModel>();

    [ValidateNever]
    public IReadOnlyList<VendorContactViewModel> Contacts { get; set; } = Array.Empty<VendorContactViewModel>();

    [ValidateNever]
    public IReadOnlyList<VendorScheduledWorkViewModel> ScheduledWorks { get; set; } = Array.Empty<VendorScheduledWorkViewModel>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> AssignableTickets { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public VendorOptionsViewModel Options { get; set; } = new();
}

public class VendorDetailsViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;
    public string Notes { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public int ActiveCategoryCount { get; set; }
    public int AssignedTicketCount { get; set; }
    public int ContactCount { get; set; }
    public int ScheduledWorkCount { get; set; }
}

public class VendorFormPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid? VendorId { get; set; }
    public VendorFormViewModel Form { get; set; } = new();
}

public class VendorDeleteViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid VendorId { get; set; }
    public string Name { get; set; } = default!;
    public string RegistryCode { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [Display(Name = "RegistryCode", ResourceType = typeof(UiText))]
    public string ConfirmationRegistryCode { get; set; } = default!;
}

public class VendorFormViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Name", ResourceType = typeof(UiText))]
    public string Name { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(50, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "RegistryCode", ResourceType = typeof(UiText))]
    public string RegistryCode { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string Notes { get; set; } = default!;
    
}

public class AddVendorCategoryViewModel
{
    [Display(Name = "TicketCategory", ResourceType = typeof(UiText))]
    public Guid TicketCategoryId { get; set; }
}

public class AddVendorContactViewModel
{
    [Display(Name = "Contacts", ResourceType = typeof(UiText))]
    public Guid ContactTypeId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(255, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Contacts", ResourceType = typeof(UiText))]
    public string ContactValue { get; set; } = default!;

    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "FullName", ResourceType = typeof(UiText))]
    public string? FullName { get; set; }

    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Role", ResourceType = typeof(UiText))]
    public string? RoleTitle { get; set; }

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? ContactNotes { get; set; }

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

public class AssignVendorTicketViewModel
{
    [Display(Name = "Tickets", ResourceType = typeof(UiText))]
    public Guid TicketId { get; set; }
}

public class AddVendorScheduledWorkViewModel
{
    [Display(Name = "Tickets", ResourceType = typeof(UiText))]
    public Guid TicketId { get; set; }

    [Display(Name = "Status", ResourceType = typeof(UiText))]
    public Guid WorkStatusId { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "DueFrom", ResourceType = typeof(UiText))]
    public DateTime ScheduledStart { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "DueTo", ResourceType = typeof(UiText))]
    public DateTime? ScheduledEnd { get; set; }

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}

public class VendorCategoryViewModel
{
    public Guid TicketCategoryId { get; set; }
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
}

public class VendorTicketViewModel
{
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string StatusLabel { get; set; } = default!;
    public string CategoryLabel { get; set; } = default!;
    public DateTime? DueAt { get; set; }
}

public class VendorContactViewModel
{
    public string ContactTypeLabel { get; set; } = default!;
    public string ContactValue { get; set; } = default!;
    public string? FullName { get; set; }
    public string? RoleTitle { get; set; }
    public bool IsPrimary { get; set; }
    public bool Confirmed { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class VendorScheduledWorkViewModel
{
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = default!;
    public string TicketTitle { get; set; } = default!;
    public string WorkStatusLabel { get; set; } = default!;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public string? Notes { get; set; }
}

public class VendorOptionsViewModel
{
    public IReadOnlyList<SelectListItem> TicketCategories { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> ContactTypes { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> WorkStatuses { get; set; } = Array.Empty<SelectListItem>();
}
