namespace App.BLL.DTO.WorkLogs.Models;

public class WorkLogListModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public Guid ScheduledWorkId { get; init; }
    public string VendorName { get; init; } = default!;
    public string WorkStatusLabel { get; init; } = default!;
    public bool CanViewCosts { get; init; }
    public WorkLogTotalsModel Totals { get; init; } = new();
    public IReadOnlyList<WorkLogListItemModel> Items { get; init; } = Array.Empty<WorkLogListItemModel>();
}

public class WorkLogFormModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public Guid ScheduledWorkId { get; init; }
    public Guid? WorkLogId { get; init; }
    public string VendorName { get; init; } = default!;
    public bool CanViewCosts { get; init; }
    public DateTime? WorkStart { get; init; }
    public DateTime? WorkEnd { get; init; }
    public decimal? Hours { get; init; }
    public decimal? MaterialCost { get; init; }
    public decimal? LaborCost { get; init; }
    public string? Description { get; init; }
}

public class WorkLogDeleteModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public Guid ScheduledWorkId { get; init; }
    public Guid WorkLogId { get; init; }
    public string VendorName { get; init; } = default!;
    public string? Description { get; init; }
}

public class WorkLogListItemModel
{
    public Guid WorkLogId { get; init; }
    public Guid AppUserId { get; init; }
    public string AppUserName { get; init; } = default!;
    public DateTime? WorkStart { get; init; }
    public DateTime? WorkEnd { get; init; }
    public decimal? Hours { get; init; }
    public decimal? MaterialCost { get; init; }
    public decimal? LaborCost { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class WorkLogTotalsModel
{
    public int Count { get; init; }
    public decimal Hours { get; init; }
    public decimal MaterialCost { get; init; }
    public decimal LaborCost { get; init; }
    public decimal TotalCost { get; init; }
}
