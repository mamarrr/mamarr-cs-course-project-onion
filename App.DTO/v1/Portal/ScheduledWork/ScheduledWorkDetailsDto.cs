namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkDetailsDto : ScheduledWorkListItemDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string ListPath { get; set; } = string.Empty;
    public string EditFormPath { get; set; } = string.Empty;
}
