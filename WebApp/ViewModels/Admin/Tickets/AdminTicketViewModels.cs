using System.ComponentModel.DataAnnotations;
using App.Resources.Views;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WebApp.ViewModels.Admin;

namespace WebApp.ViewModels.Admin.Tickets;

public class AdminTicketSearchViewModel
{
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Company))]
    public string? Company { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Customer))]
    public string? Customer { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.TicketNumber))]
    public string? TicketNumber { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Status))]
    public string? Status { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Priority))]
    public string? Priority { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Category))]
    public string? Category { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.Vendor))]
    public string? Vendor { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.CreatedFrom))]
    public DateTime? CreatedFrom { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.CreatedTo))]
    public DateTime? CreatedTo { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.DueFrom))]
    public DateTime? DueFrom { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.DueTo))]
    public DateTime? DueTo { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.OverdueOnly))]
    public bool OverdueOnly { get; set; }
    [Display(ResourceType = typeof(AdminText), Name = nameof(AdminText.OpenOnly))]
    public bool OpenOnly { get; set; }
}

public class AdminTicketListViewModel : AdminPageViewModel
{
    public AdminTicketSearchViewModel Search { get; set; } = new();
    [ValidateNever] public IReadOnlyList<AdminTicketListItemViewModel> Tickets { get; set; } = [];
}

public class AdminTicketListItemViewModel
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public bool IsOverdue { get; set; }
}

public class AdminTicketDetailsViewModel : AdminTicketListItemViewModel, IAdminPageViewModel
{
    public string PageTitle { get; set; } = string.Empty;
    public string ActiveSection { get; set; } = string.Empty;
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? PropertyLabel { get; set; }
    public string? UnitNumber { get; set; }
    public string? ResidentName { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyList<AdminScheduledWorkViewModel> ScheduledWorks { get; set; } = [];
    public IReadOnlyList<AdminWorkLogViewModel> WorkLogs { get; set; } = [];
}

public class AdminScheduledWorkViewModel
{
    public string VendorName { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
}

public class AdminWorkLogViewModel
{
    public string LoggedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
}
