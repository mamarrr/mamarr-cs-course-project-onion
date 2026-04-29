namespace App.BLL.Contracts.Leases.Models;

public sealed class ResidentLeaseListModel
{
    public IReadOnlyList<ResidentLeaseModel> Leases { get; init; } = Array.Empty<ResidentLeaseModel>();
}

public sealed class ResidentLeaseModel
{
    public Guid LeaseId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public string PropertyName { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public string UnitSlug { get; init; } = default!;
    public Guid LeaseRoleId { get; init; }
    public string LeaseRoleCode { get; init; } = default!;
    public string LeaseRoleLabel { get; init; } = default!;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class UnitLeaseListModel
{
    public IReadOnlyList<UnitLeaseModel> Leases { get; init; } = Array.Empty<UnitLeaseModel>();
}

public sealed class UnitLeaseModel
{
    public Guid LeaseId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid UnitId { get; init; }
    public Guid PropertyId { get; init; }
    public string ResidentFullName { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
    public Guid LeaseRoleId { get; init; }
    public string LeaseRoleCode { get; init; } = default!;
    public string LeaseRoleLabel { get; init; } = default!;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class LeaseModel
{
    public Guid LeaseId { get; init; }
    public Guid LeaseRoleId { get; init; }
    public Guid ResidentId { get; init; }
    public Guid UnitId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool IsActive { get; init; }
    public string? Notes { get; init; }
}

public sealed class LeaseCommandModel
{
    public Guid LeaseId { get; init; }
}

public sealed class LeasePropertySearchResultModel
{
    public IReadOnlyList<LeasePropertySearchItemModel> Properties { get; init; } = Array.Empty<LeasePropertySearchItemModel>();
}

public sealed class LeasePropertySearchItemModel
{
    public Guid PropertyId { get; init; }
    public Guid CustomerId { get; init; }
    public string PropertySlug { get; init; } = default!;
    public string PropertyName { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string City { get; init; } = default!;
    public string PostalCode { get; init; } = default!;
}

public sealed class LeaseUnitOptionsModel
{
    public IReadOnlyList<LeaseUnitOptionModel> Units { get; init; } = Array.Empty<LeaseUnitOptionModel>();
}

public sealed class LeaseUnitOptionModel
{
    public Guid UnitId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
    public int? FloorNr { get; init; }
    public bool IsActive { get; init; }
}

public sealed class LeaseResidentSearchResultModel
{
    public IReadOnlyList<LeaseResidentSearchItemModel> Residents { get; init; } = Array.Empty<LeaseResidentSearchItemModel>();
}

public sealed class LeaseResidentSearchItemModel
{
    public Guid ResidentId { get; init; }
    public string FullName { get; init; } = default!;
    public string IdCode { get; init; } = default!;
    public bool IsActive { get; init; }
}

public sealed class LeaseRoleOptionsModel
{
    public IReadOnlyList<LeaseRoleOptionModel> Roles { get; init; } = Array.Empty<LeaseRoleOptionModel>();
}

public sealed class LeaseRoleOptionModel
{
    public Guid LeaseRoleId { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
}
