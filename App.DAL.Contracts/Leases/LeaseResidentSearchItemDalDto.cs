namespace App.DAL.Contracts.DAL.Leases;

public class LeaseResidentSearchItemDalDto
{
    public Guid ResidentId { get; init; }
    public string FullName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public bool IsActive { get; init; }
}
