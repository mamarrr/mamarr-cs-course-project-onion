namespace App.BLL.DTO.Leases.Models;

public class UnitLeaseListModel
{
    public IReadOnlyList<UnitLeaseModel> Leases { get; init; } = Array.Empty<UnitLeaseModel>();
}
