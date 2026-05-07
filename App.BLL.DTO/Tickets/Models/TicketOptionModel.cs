namespace App.BLL.DTO.Tickets.Models;

public class TicketOptionModel
{
    public Guid Id { get; init; }
    public string Label { get; init; } = default!;
    public string? Code { get; init; }
}
