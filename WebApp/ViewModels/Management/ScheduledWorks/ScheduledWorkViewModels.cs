using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.ScheduledWorks;

public class ScheduledWorkIndexViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid TicketId { get; set; }

    [ValidateNever]
    public string TicketNr { get; set; } = default!;

    [ValidateNever]
    public string TicketTitle { get; set; } = default!;

    [ValidateNever]
    public IReadOnlyList<ScheduledWorkListItemViewModel> Items { get; set; } = Array.Empty<ScheduledWorkListItemViewModel>();
}

public class ScheduledWorkDetailsViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid TicketId { get; set; }
    public Guid ScheduledWorkId { get; set; }

    [ValidateNever]
    public string TicketNr { get; set; } = default!;

    [ValidateNever]
    public string TicketTitle { get; set; } = default!;

    [ValidateNever]
    public ScheduledWorkListItemViewModel Item { get; set; } = new();

    public ScheduledWorkActionDateViewModel StartForm { get; set; } = new();
    public ScheduledWorkActionDateViewModel CompleteForm { get; set; } = new();
}

public class ScheduledWorkFormPageViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid TicketId { get; set; }

    [ValidateNever]
    public string TicketNr { get; set; } = default!;

    [ValidateNever]
    public string TicketTitle { get; set; } = default!;

    public bool IsEdit { get; set; }
    public ScheduledWorkFormViewModel Form { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> Vendors { get; set; } = Array.Empty<SelectListItem>();

    [ValidateNever]
    public IReadOnlyList<SelectListItem> WorkStatuses { get; set; } = Array.Empty<SelectListItem>();
}

public class ScheduledWorkDeleteViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid TicketId { get; set; }
    public Guid ScheduledWorkId { get; set; }

    [ValidateNever]
    public string TicketNr { get; set; } = default!;

    [ValidateNever]
    public string VendorName { get; set; } = default!;
}

public class ScheduledWorkFormViewModel
{
    public Guid? ScheduledWorkId { get; set; }

    [Display(Name = "Vendors", ResourceType = typeof(UiText))]
    public Guid VendorId { get; set; }

    public Guid WorkStatusId { get; set; }

    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [DataType(DataType.DateTime)]
    public DateTime ScheduledStart { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? ScheduledEnd { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? RealStart { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? RealEnd { get; set; }

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    [Display(Name = "Notes", ResourceType = typeof(UiText))]
    public string? Notes { get; set; }
}

public class ScheduledWorkActionDateViewModel
{
    [Required(ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.RequiredField))]
    [DataType(DataType.DateTime)]
    public DateTime ActionAt { get; set; } = DateTime.UtcNow;
}

public class ScheduledWorkListItemViewModel
{
    public Guid ScheduledWorkId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = default!;
    public Guid WorkStatusId { get; set; }
    public string WorkStatusCode { get; set; } = default!;
    public string WorkStatusLabel { get; set; } = default!;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int WorkLogCount { get; set; }
}
