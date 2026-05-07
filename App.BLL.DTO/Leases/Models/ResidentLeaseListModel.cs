namespace App.BLL.DTO.Leases.Models;

public class ResidentLeaseListModel
{
    public IReadOnlyList<ResidentLeaseModel> Leases { get; init; } = Array.Empty<ResidentLeaseModel>();
}
