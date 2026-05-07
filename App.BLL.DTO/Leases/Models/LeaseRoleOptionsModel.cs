namespace App.BLL.DTO.Leases.Models;

public class LeaseRoleOptionsModel
{
    public IReadOnlyList<LeaseRoleOptionModel> Roles { get; init; } = Array.Empty<LeaseRoleOptionModel>();
}
