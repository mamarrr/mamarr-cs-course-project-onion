namespace App.BLL.DTO.Admin.Tickets;

public class AdminTicketSearchDto
{
    public string? Company { get; set; }
    public string? Customer { get; set; }
    public string? TicketNumber { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public string? Vendor { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
    public bool OverdueOnly { get; set; }
    public bool OpenOnly { get; set; }
}

public class AdminTicketListDto
{
    public AdminTicketSearchDto Search { get; set; } = new();
    public IReadOnlyList<AdminTicketListItemDto> Tickets { get; set; } = [];
}

public class AdminTicketListItemDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsOverdue { get; set; }
}

public class AdminTicketDetailsDto : AdminTicketListItemDto
{
    public string Description { get; set; } = string.Empty;
    public string? PropertyLabel { get; set; }
    public string? UnitNumber { get; set; }
    public string? ResidentName { get; set; }
    public IReadOnlyList<AdminScheduledWorkDto> ScheduledWorks { get; set; } = [];
    public IReadOnlyList<AdminWorkLogDto> WorkLogs { get; set; } = [];
}

public class AdminScheduledWorkDto
{
    public Guid Id { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
}

public class AdminWorkLogDto
{
    public Guid Id { get; set; }
    public string LoggedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
}
