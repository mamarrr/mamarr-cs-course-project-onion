namespace App.DTO.v1.Portal.Tickets;

public class TicketDetailsDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitNr { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? VendorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? NextStatusCode { get; set; }
    public string? NextStatusLabel { get; set; }
    public bool CanAdvanceStatus { get; set; }
    public IReadOnlyList<string> TransitionBlockingReasons { get; set; } = [];
    public IReadOnlyList<TicketScheduledWorkListItemDto> ScheduledWork { get; set; } = [];
}
