namespace App.DTO.v1.Portal.Tickets;

public class TicketSelectorOptionsQueryDto
{
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? CategoryId { get; set; }
}
