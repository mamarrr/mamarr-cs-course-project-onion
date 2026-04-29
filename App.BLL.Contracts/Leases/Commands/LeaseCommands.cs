using App.BLL.Contracts.Leases.Queries;

namespace App.BLL.Contracts.Leases.Commands;

public sealed class CreateLeaseFromResidentCommand : GetResidentLeasesQuery
{
    public Guid UnitId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreateLeaseFromUnitCommand : GetUnitLeasesQuery
{
    public Guid ResidentId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateLeaseFromResidentCommand : GetResidentLeaseQuery
{
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateLeaseFromUnitCommand : GetUnitLeaseQuery
{
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class DeleteLeaseFromResidentCommand : GetResidentLeaseQuery;

public sealed class DeleteLeaseFromUnitCommand : GetUnitLeaseQuery;
