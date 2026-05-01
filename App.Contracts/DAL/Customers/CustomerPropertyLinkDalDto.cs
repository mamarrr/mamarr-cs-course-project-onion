namespace App.Contracts.DAL.Customers;

public class CustomerPropertyLinkDalDto
{
    public Guid CustomerId { get; init; }
    public string PropertySlug { get; init; } = default!;
    public string PropertyName { get; init; } = default!;
}
