using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.UI.Chrome;

namespace WebApp.ViewModels.Management.WorkLogs;

public class WorkLogIndexViewModel : IAppChromePage
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
    public string VendorName { get; set; } = default!;

    [ValidateNever]
    public string WorkStatusLabel { get; set; } = default!;

    public bool CanViewCosts { get; set; }
    public WorkLogTotalsViewModel Totals { get; set; } = new();

    [ValidateNever]
    public IReadOnlyList<WorkLogListItemViewModel> Items { get; set; } = Array.Empty<WorkLogListItemViewModel>();
}

public class WorkLogFormPageViewModel : IAppChromePage
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
    public string VendorName { get; set; } = default!;

    public bool CanViewCosts { get; set; }
    public bool IsEdit { get; set; }
    public WorkLogFormViewModel Form { get; set; } = new();
}

public class WorkLogDeleteViewModel : IAppChromePage
{
    [ValidateNever]
    public AppChromeViewModel AppChrome { get; init; } = new();

    [ValidateNever]
    public string CompanySlug { get; set; } = default!;

    [ValidateNever]
    public string CompanyName { get; set; } = default!;

    public Guid TicketId { get; set; }
    public Guid ScheduledWorkId { get; set; }
    public Guid WorkLogId { get; set; }

    [ValidateNever]
    public string TicketNr { get; set; } = default!;

    [ValidateNever]
    public string VendorName { get; set; } = default!;

    [ValidateNever]
    public string? Description { get; set; }
}

public class WorkLogFormViewModel
{
    public Guid? WorkLogId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? WorkStart { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? WorkEnd { get; set; }

    [Range(typeof(decimal), "0", "99999999.99", ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidData))]
    public decimal? Hours { get; set; }

    [Range(typeof(decimal), "0", "99999999.99", ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidData))]
    public decimal? MaterialCost { get; set; }

    [Range(typeof(decimal), "0", "99999999.99", ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.InvalidData))]
    public decimal? LaborCost { get; set; }

    [StringLength(4000, MinimumLength = 1, ErrorMessageResourceType = typeof(UiText), ErrorMessageResourceName = nameof(UiText.StringLengthBetween))]
    public string? Description { get; set; }
}

public class WorkLogListItemViewModel
{
    public Guid WorkLogId { get; set; }
    public string AppUserName { get; set; } = default!;
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WorkLogTotalsViewModel
{
    public int Count { get; set; }
    public decimal Hours { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal TotalCost { get; set; }
}
