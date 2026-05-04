namespace App.BLL.Contracts.Customers.Queries;

public class GetCustomerWorkspaceQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CustomerSlug { get; init; } = default!;
}
