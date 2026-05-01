namespace App.BLL.Contracts.Tickets.Queries;

public class GetManagementTicketsQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string? Search { get; init; }
    public Guid? StatusId { get; init; }
    public Guid? PriorityId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueFrom { get; init; }
    public DateTime? DueTo { get; init; }
}

public class GetManagementTicketQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid TicketId { get; init; }
}

public class GetManagementTicketSelectorOptionsQuery
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? CategoryId { get; init; }
}
