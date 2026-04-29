namespace App.BLL.Contracts.Customers.Queries;

public sealed class GetCompanyCustomersQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
}
