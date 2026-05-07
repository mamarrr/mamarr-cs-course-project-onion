namespace App.BLL.DTO.Leases.Models;

public class LeaseRoleOptionModel
{
    public Guid LeaseRoleId { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
