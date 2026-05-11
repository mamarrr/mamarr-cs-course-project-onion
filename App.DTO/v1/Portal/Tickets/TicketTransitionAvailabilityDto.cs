namespace App.DTO.v1.Portal.Tickets;

public class TicketTransitionAvailabilityDto
{
    public Guid TicketId { get; set; }
    public string CurrentStatusCode { get; set; } = string.Empty;
    public string? NextStatusCode { get; set; }
    public string? NextStatusLabel { get; set; }
    public bool CanAdvance { get; set; }
    public IReadOnlyList<string> BlockingReasons { get; set; } = [];
}
