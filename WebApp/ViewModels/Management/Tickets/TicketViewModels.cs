using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;
using WebApp.ViewModels.Management.ScheduledWorks;

namespace WebApp.ViewModels.Management.Tickets;

public class TicketsPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public TicketFilterViewModel Filter { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<TicketListItemViewModel> Tickets { get; set; } = Array.Empty<TicketListItemViewModel>();

    [ValidateNever]
    public TicketSelectOptionsViewModel Options { get; set; } = new();
}

public class TicketListItemViewModel
{
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string StatusCode { get; set; } = default!;
    public string StatusLabel { get; set; } = default!;
    public string PriorityLabel { get; set; } = default!;
    public string CategoryLabel { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitNr { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? VendorName { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketFilterViewModel
{
    [Display(Name = "Search", ResourceType = typeof(UiText))]
    public string? Search { get; set; }

    [Display(Name = "Status", ResourceType = typeof(UiText))]
    public Guid? StatusId { get; set; }

    [Display(Name = "TicketPriority", ResourceType = typeof(UiText))]
    public Guid? PriorityId { get; set; }

    [Display(Name = "TicketCategory", ResourceType = typeof(UiText))]
    public Guid? CategoryId { get; set; }

    [Display(Name = "Customers", ResourceType = typeof(UiText))]
    public Guid? CustomerId { get; set; }

    [Display(Name = "Property", ResourceType = typeof(UiText))]
    public Guid? PropertyId { get; set; }

    [Display(Name = "Unit", ResourceType = typeof(UiText))]
    public Guid? UnitId { get; set; }

    [Display(Name = "Vendors", ResourceType = typeof(UiText))]
    public Guid? VendorId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "DueFrom", ResourceType = typeof(UiText))]
    public DateTime? DueFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "DueTo", ResourceType = typeof(UiText))]
    public DateTime? DueTo { get; set; }
}

public class TicketFormPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public bool IsEdit { get; set; }

    public TicketFormViewModel Form { get; set; } = new();

    [ValidateNever]
    public TicketSelectOptionsViewModel Options { get; set; } = new();
}

public class TicketFormViewModel
{
    public Guid? TicketId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(20, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "TicketNumber", ResourceType = typeof(UiText))]
    public string TicketNr { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(200, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "TicketTitle", ResourceType = typeof(UiText))]
    public string Title { get; set; } = default!;

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "TicketDescription", ResourceType = typeof(UiText))]
    public string Description { get; set; } = default!;

    [Display(Name = "TicketCategory", ResourceType = typeof(UiText))]
    public Guid TicketCategoryId { get; set; }

    [Display(Name = "Status", ResourceType = typeof(UiText))]
    public Guid TicketStatusId { get; set; }

    [Display(Name = "TicketPriority", ResourceType = typeof(UiText))]
    public Guid TicketPriorityId { get; set; }

    [Display(Name = "Customers", ResourceType = typeof(UiText))]
    public Guid? CustomerId { get; set; }

    [Display(Name = "Property", ResourceType = typeof(UiText))]
    public Guid? PropertyId { get; set; }

    [Display(Name = "Unit", ResourceType = typeof(UiText))]
    public Guid? UnitId { get; set; }

    [Display(Name = "Resident", ResourceType = typeof(UiText))]
    public Guid? ResidentId { get; set; }

    [Display(Name = "Vendors", ResourceType = typeof(UiText))]
    public Guid? VendorId { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "DueAt", ResourceType = typeof(UiText))]
    public DateTime? DueAt { get; set; }
}

public class TicketDetailsPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string StatusCode { get; set; } = default!;
    public string StatusLabel { get; set; } = default!;
    public string PriorityLabel { get; set; } = default!;
    public string CategoryLabel { get; set; } = default!;
    public string? CustomerName { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitNr { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? VendorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? NextStatusCode { get; set; }
    public string? NextStatusLabel { get; set; }
    public bool CanAdvanceStatus { get; set; }
    public IReadOnlyList<string> TransitionBlockingReasons { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ScheduledWorkListItemViewModel> ScheduledWork { get; set; } =
        Array.Empty<ScheduledWorkListItemViewModel>();
}

public class TicketSelectOptionsViewModel
{
    public IReadOnlyList<SelectListItem> Statuses { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Priorities { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Customers { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Properties { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Units { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Residents { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyList<SelectListItem> Vendors { get; set; } = Array.Empty<SelectListItem>();
}

public class TicketOptionJsonViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = default!;
}
