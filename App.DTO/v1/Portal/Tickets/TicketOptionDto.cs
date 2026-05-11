namespace App.DTO.v1.Portal.Tickets;

public class TicketOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Code { get; set; }
}
