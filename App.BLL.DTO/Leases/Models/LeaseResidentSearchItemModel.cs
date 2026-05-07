namespace App.BLL.DTO.Leases.Models;

public class LeaseResidentSearchItemModel
{
    public Guid ResidentId { get; init; }
    public string FullName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
}
