using Base.Domain;

namespace App.BLL.Management;

public class ManagementResidentLeaseListResult
{
    public IReadOnlyList<ManagementResidentLeaseListItem> Leases { get; set; } = Array.Empty<ManagementResidentLeaseListItem>();
}

public class ManagementResidentLeaseListItem
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

public class ManagementUnitLeaseListResult
{
    public IReadOnlyList<ManagementUnitLeaseListItem> Leases { get; set; } = Array.Empty<ManagementUnitLeaseListItem>();
}

public class ManagementUnitLeaseListItem
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

public class ManagementLeaseDetailsResult
{
    public bool Success { get; set; }
    public bool LeaseNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementLeaseDetails? Lease { get; set; }
}

public class ManagementLeaseDetails
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

public class ManagementLeaseCreateRequest
{
    public Guid LeaseRoleId { get; set; }
    public Guid UnitId { get; set; }
    public Guid ResidentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class ManagementLeaseUpdateRequest
{
    public Guid LeaseId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
}

public class ManagementLeaseDeleteRequest
{
    public Guid LeaseId { get; set; }
}

public class ManagementLeaseCommandResult
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

public class ManagementLeasePropertySearchResult
{
    public IReadOnlyList<ManagementLeasePropertySearchItem> Properties { get; set; } = Array.Empty<ManagementLeasePropertySearchItem>();
}

public class ManagementLeasePropertySearchItem
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

public class ManagementLeaseUnitOptionsResult
{
    public bool Success { get; set; }
    public bool PropertyNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<ManagementLeaseUnitOption> Units { get; set; } = Array.Empty<ManagementLeaseUnitOption>();
}

public class ManagementLeaseUnitOption
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = default!;
    public string UnitNr { get; set; } = default!;
    public int? FloorNr { get; set; }
    public bool IsActive { get; set; }
}

public class ManagementLeaseResidentSearchResult
{
    public IReadOnlyList<ManagementLeaseResidentSearchItem> Residents { get; set; } = Array.Empty<ManagementLeaseResidentSearchItem>();
}

public class ManagementLeaseResidentSearchItem
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = default!;
    public string IdCode { get; set; } = default!;
    public bool IsActive { get; set; }
}

public class ManagementLeaseRoleOptionsResult
{
    public IReadOnlyList<ManagementLeaseRoleOption> Roles { get; set; } = Array.Empty<ManagementLeaseRoleOption>();
}

public class ManagementLeaseRoleOption
{
    public Guid LeaseRoleId { get; set; }
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
}
