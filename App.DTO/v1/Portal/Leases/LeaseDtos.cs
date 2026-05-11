namespace App.DTO.v1.Portal.Leases;

public class CreateResidentLeaseDto
{
    public Guid UnitId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class CreateUnitLeaseDto
{
    public Guid ResidentId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLeaseDto
{
    public Guid LeaseRoleId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}

public class LeaseDto
{
    public Guid LeaseId { get; set; }
    public Guid LeaseRoleId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class ResidentLeaseListItemDto
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public string UnitSlug { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = string.Empty;
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class UnitLeaseListItemDto
{
    public Guid LeaseId { get; set; }
    public Guid ResidentId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public string ResidentFullName { get; set; } = string.Empty;
    public string ResidentIdCode { get; set; } = string.Empty;
    public Guid LeaseRoleId { get; set; }
    public string LeaseRoleCode { get; set; } = string.Empty;
    public string LeaseRoleLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class LeasePropertySearchResultDto
{
    public IReadOnlyList<LeasePropertySearchItemDto> Properties { get; set; } = [];
}

public class LeasePropertySearchItemDto
{
    public Guid PropertyId { get; set; }
    public Guid CustomerId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class LeaseUnitOptionsDto
{
    public IReadOnlyList<LeaseUnitOptionDto> Units { get; set; } = [];
}

public class LeaseUnitOptionDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
}

public class LeaseResidentSearchResultDto
{
    public IReadOnlyList<LeaseResidentSearchItemDto> Residents { get; set; } = [];
}

public class LeaseResidentSearchItemDto
{
    public Guid ResidentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdCode { get; set; } = string.Empty;
}

public class LeaseRoleOptionsDto
{
    public IReadOnlyList<LeaseRoleOptionDto> Roles { get; set; } = [];
}

public class LeaseRoleOptionDto
{
    public Guid LeaseRoleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
