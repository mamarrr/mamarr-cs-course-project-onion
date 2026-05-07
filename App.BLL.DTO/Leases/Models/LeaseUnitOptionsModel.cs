namespace App.BLL.DTO.Leases.Models;

public class LeaseUnitOptionsModel
{
    public IReadOnlyList<LeaseUnitOptionModel> Units { get; init; } = Array.Empty<LeaseUnitOptionModel>();
}
