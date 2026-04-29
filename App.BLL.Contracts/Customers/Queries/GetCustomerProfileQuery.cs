namespace App.BLL.Contracts.Customers.Queries;

public sealed class GetCustomerProfileQuery
{
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
    public Guid UserId { get; init; }
}
