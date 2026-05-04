namespace App.BLL.Contracts.Vendors.Queries;

public class GetManagementVendorsQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string? Search { get; init; }
    public bool IncludeInactive { get; init; }
    public Guid? TicketCategoryId { get; init; }
}

public class GetManagementVendorQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid VendorId { get; init; }
    public string? TicketSearch { get; init; }
}
