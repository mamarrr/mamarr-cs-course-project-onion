namespace App.DAL.DTO.Leases;

public class LeasePropertySearchItemDalDto
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
