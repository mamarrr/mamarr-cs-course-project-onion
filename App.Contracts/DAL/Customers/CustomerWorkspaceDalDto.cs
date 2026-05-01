namespace App.Contracts.DAL.Customers;

public class CustomerWorkspaceDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public bool IsActive { get; init; }
}

public class CustomerUserContextDalDto
{
    public Guid CustomerId { get; init; }
    public string Name { get; init; } = default!;
}
