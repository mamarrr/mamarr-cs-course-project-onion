namespace App.BLL.Contracts.Tickets.Models;

public class ManagementTicketsModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public IReadOnlyList<ManagementTicketListItemModel> Tickets { get; init; } = Array.Empty<ManagementTicketListItemModel>();
    public TicketFilterModel Filter { get; init; } = new();
    public TicketSelectorOptionsModel Options { get; init; } = new();
}

public class ManagementTicketListItemModel
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public string PriorityLabel { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public string? CustomerName { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertyName { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitNr { get; init; }
    public string? UnitSlug { get; init; }
    public string? ResidentName { get; init; }
    public string? ResidentIdCode { get; init; }
    public string? VendorName { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ManagementTicketDetailsModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public string PriorityLabel { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public string? CustomerName { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertyName { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitNr { get; init; }
    public string? UnitSlug { get; init; }
    public string? ResidentName { get; init; }
    public string? ResidentIdCode { get; init; }
    public string? VendorName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public string? NextStatusCode { get; init; }
    public string? NextStatusLabel { get; init; }
}

public class ManagementTicketFormModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid? TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public Guid TicketCategoryId { get; init; }
    public Guid TicketStatusId { get; init; }
    public Guid TicketPriorityId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ResidentId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueAt { get; init; }
    public TicketSelectorOptionsModel Options { get; init; } = new();
}

public class TicketFilterModel
{
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

public class TicketSelectorOptionsModel
{
    public IReadOnlyList<TicketOptionModel> Statuses { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Priorities { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Categories { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Customers { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Properties { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Units { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Residents { get; init; } = Array.Empty<TicketOptionModel>();
    public IReadOnlyList<TicketOptionModel> Vendors { get; init; } = Array.Empty<TicketOptionModel>();
}

public class TicketOptionModel
{
    public Guid Id { get; init; }
    public string Label { get; init; } = default!;
    public string? Code { get; init; }
}
