using App.BLL.DTO.Tickets.Models;

namespace App.BLL.DTO.ScheduledWorks.Models;

public class ScheduledWorkListModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public IReadOnlyList<ScheduledWorkListItemModel> Items { get; init; } = Array.Empty<ScheduledWorkListItemModel>();
}

public class ScheduledWorkDetailsModel : ScheduledWorkListItemModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
}

public class ScheduledWorkFormModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public Guid? ScheduledWorkId { get; init; }
    public Guid VendorId { get; init; }
    public Guid WorkStatusId { get; init; }
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public DateTime? RealStart { get; init; }
    public DateTime? RealEnd { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<TicketOptionModel> Vendors { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> WorkStatuses { get; init; } = Array.Empty<TicketOptionModel>();
}

public class ScheduledWorkListItemModel
{
    public Guid ScheduledWorkId { get; init; }
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = default!;
    public Guid WorkStatusId { get; init; }
    public string WorkStatusCode { get; init; } = default!;
    public string WorkStatusLabel { get; init; } = default!;
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public DateTime? RealStart { get; init; }
    public DateTime? RealEnd { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public int WorkLogCount { get; init; }
}
