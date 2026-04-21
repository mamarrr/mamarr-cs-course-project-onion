namespace App.BLL.LeaseAssignments;

public class ResidentLeaseListResult
{
    public IReadOnlyList<ResidentLeaseListItem> Leases { get; set; } = Array.Empty<ResidentLeaseListItem>();
}

public class ResidentLeaseListItem
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = default!;
    public string PropertySlug { get; set; } = default!;
    public string UnitNr { get; set; } = default!;
    public string UnitSlug { get; set; } = default!;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = default!;
    public string LeaseRoleLabel { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class UnitLeaseListResult
{
    public IReadOnlyList<UnitLeaseListItem> Leases { get; set; } = Array.Empty<UnitLeaseListItem>();
}

public class UnitLeaseListItem
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string ResidentFullName { get; set; } = default!;
    public string ResidentIdCode { get; set; } = default!;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = default!;
    public string LeaseRoleLabel { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class LeaseDetailsResult
{
    public bool Success { get; set; }
    public bool LeaseNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public LeaseDetails? Lease { get; set; }
}

public class LeaseDetails
{
    public Guid LeaseId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class LeaseCreateRequest
{
    public Guid LeaseRoleId { get; set; }
    public Guid UnitId { get; set; }
    public Guid ResidentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class LeaseUpdateRequest
{
    public Guid LeaseId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class LeaseDeleteRequest
{
    public Guid LeaseId { get; set; }
}

public class LeaseCommandResult
{
    public bool Success { get; set; }
    public bool LeaseNotFound { get; set; }
    public bool ResidentNotFound { get; set; }
    public bool UnitNotFound { get; set; }
    public bool PropertyNotFound { get; set; }
    public bool InvalidLeaseRole { get; set; }
    public bool InvalidStartDate { get; set; }
    public bool InvalidEndDate { get; set; }
    public bool DuplicateActiveLease { get; set; }
    public Guid? LeaseId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LeasePropertySearchResult
{
    public IReadOnlyList<LeasePropertySearchItem> Properties { get; set; } = Array.Empty<LeasePropertySearchItem>();
}

public class LeasePropertySearchItem
{
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public string PropertySlug { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
    public string CustomerSlug { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string City { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
}

public class LeaseUnitOptionsResult
{
    public bool Success { get; set; }
    public bool PropertyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<LeaseUnitOption> Units { get; set; } = Array.Empty<LeaseUnitOption>();
}

public class LeaseUnitOption
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = default!;
    public string UnitNr { get; set; } = default!;
    public int? FloorNr { get; set; }
    public bool IsActive { get; set; }
}

public class LeaseResidentSearchResult
{
    public IReadOnlyList<LeaseResidentSearchItem> Residents { get; set; } = Array.Empty<LeaseResidentSearchItem>();
}

public class LeaseResidentSearchItem
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class LeaseRoleOptionsResult
{
    public IReadOnlyList<LeaseRoleOption> Roles { get; set; } = Array.Empty<LeaseRoleOption>();
}

public class LeaseRoleOption
{
    public Guid LeaseRoleId { get; set; }
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
}
