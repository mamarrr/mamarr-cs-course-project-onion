using Base.Domain;

namespace App.DAL.DTO.ScheduledWorks;

public class ScheduledWorkDalDto : BaseEntity
{
    public Guid VendorId { get; set; }
    public Guid TicketId { get; set; }
    public Guid WorkStatusId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
}

public class ScheduledWorkListItemDalDto
{
    public Guid Id { get; init; }
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

public class ScheduledWorkDetailsDalDto : ScheduledWorkListItemDalDto
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
}
