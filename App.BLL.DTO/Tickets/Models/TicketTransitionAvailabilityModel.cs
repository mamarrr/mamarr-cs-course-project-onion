using Base.Domain;

namespace App.BLL.DTO.Tickets.Models;

public class TicketTransitionAvailabilityModel : BaseEntity
{
    public Guid TicketId { get; init; }
    public string CurrentStatusCode { get; init; } = default!;
    public string? NextStatusCode { get; init; }
    public string? NextStatusLabel { get; init; }
    public bool CanAdvance { get; init; }
    public IReadOnlyList<string> BlockingReasons { get; init; } = Array.Empty<string>();
}
