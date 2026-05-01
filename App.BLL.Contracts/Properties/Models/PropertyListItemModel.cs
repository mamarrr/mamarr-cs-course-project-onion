namespace App.BLL.Contracts.Properties.Models;

public class PropertyListItemModel
{
    public Guid PropertyId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string PropertyName { get; init; } = default!;
    public string PropertySlug { get; init; } = default!;
    public string AddressLine { get; init; } = default!;
    public string City { get; init; } = default!;
    public string PostalCode { get; init; } = default!;
    public Guid PropertyTypeId { get; init; }
    public string PropertyTypeCode { get; init; } = default!;
    public string PropertyTypeLabel { get; init; } = default!;
    public bool IsActive { get; init; }
}
