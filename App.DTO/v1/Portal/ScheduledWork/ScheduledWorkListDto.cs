namespace App.DTO.v1.Portal.ScheduledWork;

public class ScheduledWorkListDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public IReadOnlyList<ScheduledWorkListItemDto> Items { get; set; } = [];
}
