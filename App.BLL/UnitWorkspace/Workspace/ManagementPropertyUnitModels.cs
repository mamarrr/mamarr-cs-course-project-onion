namespace App.BLL.UnitWorkspace.Workspace;

public class ManagementPropertyUnitListResult
{
    public IReadOnlyList<ManagementPropertyUnitListItem> Units { get; set; } = Array.Empty<ManagementPropertyUnitListItem>();
}

public class ManagementPropertyUnitListItem
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = default!;
    public string UnitNr { get; set; } = default!;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
}

public class ManagementPropertyUnitCreateRequest
{
    public string UnitNr { get; set; } = default!;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public string? Notes { get; set; }
}

public class ManagementPropertyUnitCreateResult
{
    public bool Success { get; set; }
    public bool InvalidUnitNr { get; set; }
    public bool InvalidFloorNr { get; set; }
    public bool InvalidSizeM2 { get; set; }
    public Guid? CreatedUnitId { get; set; }
    public string? CreatedUnitSlug { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManagementUnitDashboardAccessResult
{
    public bool IsAuthorized { get; set; }
    public bool UnitNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public ManagementUnitDashboardContext? Context { get; set; }
}

public class ManagementUnitDashboardContext
{
    public Guid AppUserId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = default!;
    public string CompanyName { get; set; } = default!;
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = default!;
    public string UnitNr { get; set; } = default!;
}
