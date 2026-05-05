using App.BLL.Contracts.Leases.Queries;

namespace App.BLL.Contracts.Leases.Commands;

public class CreateLeaseFromResidentCommand : GetResidentLeasesQuery
{
    public Guid UnitId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}

public class CreateLeaseFromUnitCommand : GetUnitLeasesQuery
{
    public Guid ResidentId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}

public class UpdateLeaseFromResidentCommand : GetResidentLeaseQuery
{
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}

public class UpdateLeaseFromUnitCommand : GetUnitLeaseQuery
{
    public Guid LeaseRoleId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? Notes { get; init; }
}

public class DeleteLeaseFromResidentCommand : GetResidentLeaseQuery;

public class DeleteLeaseFromUnitCommand : GetUnitLeaseQuery;
