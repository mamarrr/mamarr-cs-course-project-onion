namespace App.DAL.Contracts.DAL.Leases;

public class LeaseRoleOptionDalDto
{
    public Guid LeaseRoleId { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
