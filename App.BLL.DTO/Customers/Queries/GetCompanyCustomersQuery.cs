namespace App.BLL.DTO.Customers.Queries;

public class GetCompanyCustomersQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
}
