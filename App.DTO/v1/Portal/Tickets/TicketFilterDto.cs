namespace App.DTO.v1.Portal.Tickets;

public class TicketFilterDto
{
    public string? Search { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
}
