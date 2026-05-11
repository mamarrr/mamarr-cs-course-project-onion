namespace App.DTO.v1.Portal.WorkLogs;

public class WorkLogListDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public Guid ScheduledWorkId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public bool CanViewCosts { get; set; }
    public WorkLogTotalsDto Totals { get; set; } = new();
    public IReadOnlyList<WorkLogListItemDto> Items { get; set; } = [];
    public string Path { get; set; } = string.Empty;
}
