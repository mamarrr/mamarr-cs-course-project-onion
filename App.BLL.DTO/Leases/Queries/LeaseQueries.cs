namespace App.BLL.DTO.Leases.Queries;

public class GetResidentLeasesQuery
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid ResidentId { get; init; }
    public string ResidentIdCode { get; init; } = default!;
    public string FullName { get; init; } = default!;
}

public class GetUnitLeasesQuery
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid CustomerId { get; init; }
    public string CustomerSlug { get; init; } = default!;
    public string CustomerName { get; init; } = default!;
    public Guid PropertyId { get; init; }
    public string PropertySlug { get; init; } = default!;
    public string PropertyName { get; init; } = default!;
    public Guid UnitId { get; init; }
    public string UnitSlug { get; init; } = default!;
    public string UnitNr { get; init; } = default!;
}

public class GetResidentLeaseQuery : GetResidentLeasesQuery
{
    public Guid LeaseId { get; init; }
}

public class GetUnitLeaseQuery : GetUnitLeasesQuery
{
    public Guid LeaseId { get; init; }
}

public class SearchLeasePropertiesQuery : GetResidentLeasesQuery
{
    public string? SearchTerm { get; init; }
}

public class GetLeaseUnitsForPropertyQuery : GetResidentLeasesQuery
{
    public Guid PropertyId { get; init; }
}

public class SearchLeaseResidentsQuery : GetUnitLeasesQuery
{
    public string? SearchTerm { get; init; }
}
