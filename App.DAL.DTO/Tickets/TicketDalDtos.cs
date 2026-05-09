using Base.Domain;

namespace App.DAL.DTO.Tickets;

public class TicketDalDto : BaseEntity
{
    public Guid ManagementCompanyId { get; init; }
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
    public DateTime? ClosedAt { get; init; }
}

public class TicketListFilterDalDto
{
    public string? Search { get; init; }
    public Guid? StatusId { get; init; }
    public Guid? PriorityId { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ResidentId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueFrom { get; init; }
    public DateTime? DueTo { get; init; }
}

public class TicketListItemDalDto
{
    public Guid Id { get; init; }
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

public class TicketDetailsDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public Guid TicketStatusId { get; init; }
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public Guid TicketPriorityId { get; init; }
    public string PriorityLabel { get; init; } = default!;
    public Guid TicketCategoryId { get; init; }
    public string CategoryLabel { get; init; } = default!;
    public Guid? CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerSlug { get; init; }
    public Guid? PropertyId { get; init; }
    public string? PropertyName { get; init; }
    public string? PropertySlug { get; init; }
    public Guid? UnitId { get; init; }
    public string? UnitNr { get; init; }
    public string? UnitSlug { get; init; }
    public Guid? ResidentId { get; init; }
    public string? ResidentName { get; init; }
    public string? ResidentIdCode { get; init; }
    public Guid? VendorId { get; init; }
    public string? VendorName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public class TicketEditDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public Guid TicketStatusId { get; init; }
    public string StatusCode { get; init; } = default!;
    public Guid TicketPriorityId { get; init; }
    public Guid TicketCategoryId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ResidentId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public class TicketStatusUpdateDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public Guid TicketStatusId { get; init; }
    public DateTime? ClosedAt { get; init; }
}

public class TicketOptionDalDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = default!;
    public string? Code { get; init; }
}
