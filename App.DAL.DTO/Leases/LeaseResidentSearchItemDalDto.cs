namespace App.DAL.DTO.Leases;

public class LeaseResidentSearchItemDalDto
{
    public Guid ResidentId { get; init; }
    public string FullName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    
}
