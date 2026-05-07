namespace App.BLL.DTO.Leases.Models;

public class LeaseResidentSearchResultModel
{
    public IReadOnlyList<LeaseResidentSearchItemModel> Residents { get; init; } = Array.Empty<LeaseResidentSearchItemModel>();
}
