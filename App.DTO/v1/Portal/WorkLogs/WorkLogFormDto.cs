namespace App.DTO.v1.Portal.WorkLogs;

public class WorkLogFormDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public Guid ScheduledWorkId { get; set; }
    public Guid? WorkLogId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public bool CanViewCosts { get; set; }
    public DateTime? WorkStart { get; set; }
    public DateTime? WorkEnd { get; set; }
    public decimal? Hours { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public string? Description { get; set; }
    public string Path { get; set; } = string.Empty;
}
