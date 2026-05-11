using App.DTO.v1.Shared;

namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkFormDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public Guid? ScheduledWorkId { get; set; }
    public Guid VendorId { get; set; }
    public Guid WorkStatusId { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<LookupOptionDto> Vendors { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> WorkStatuses { get; set; } = [];
}
