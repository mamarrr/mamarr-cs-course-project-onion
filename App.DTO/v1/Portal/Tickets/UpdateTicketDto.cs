namespace App.DTO.v1.Portal.Tickets;

public class UpdateTicketDto : CreateTicketDto
{
    public Guid TicketStatusId { get; set; }
}
