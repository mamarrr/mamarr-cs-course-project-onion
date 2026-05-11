namespace App.DTO.v1.Portal.Tickets;

public class TicketSearchQueryDto
{
    public string? Search { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
}

public class TicketSelectorOptionsQueryDto
{
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? CategoryId { get; set; }
}

public class CreateTicketDto
{
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TicketCategoryId { get; set; }
    public Guid TicketPriorityId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueAt { get; set; }
}

public class UpdateTicketDto : CreateTicketDto
{
    public Guid TicketStatusId { get; set; }
}

public class TicketDto
{
    public Guid TicketId { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TicketCategoryId { get; set; }
    public Guid TicketStatusId { get; set; }
    public Guid TicketPriorityId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class ManagementTicketsDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public IReadOnlyList<TicketListItemDto> Tickets { get; set; } = [];
    public TicketFilterDto Filter { get; set; } = new();
    public TicketSelectorOptionsDto Options { get; set; } = new();
}

public class ContextTicketsDto : ManagementTicketsDto
{
    public string ContextName { get; set; } = string.Empty;
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
    public string? PropertySlug { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitSlug { get; set; }
    public string? UnitName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? ResidentName { get; set; }
}

public class TicketListItemDto
{
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitNr { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? VendorName { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TicketFilterDto
{
    public string? Search { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
}

public class TicketDetailsDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public string CategoryLabel { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitNr { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? VendorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? NextStatusCode { get; set; }
    public string? NextStatusLabel { get; set; }
    public bool CanAdvanceStatus { get; set; }
    public IReadOnlyList<string> TransitionBlockingReasons { get; set; } = [];
    public IReadOnlyList<TicketScheduledWorkListItemDto> ScheduledWork { get; set; } = [];
}

public class TicketFormDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid? TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TicketCategoryId { get; set; }
    public Guid TicketStatusId { get; set; }
    public Guid TicketPriorityId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? VendorId { get; set; }
    public DateTime? DueAt { get; set; }
    public TicketSelectorOptionsDto Options { get; set; } = new();
}

public class TicketSelectorOptionsDto
{
    public IReadOnlyList<TicketOptionDto> Statuses { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Priorities { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Categories { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Customers { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Properties { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Units { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Residents { get; set; } = [];
    public IReadOnlyList<TicketOptionDto> Vendors { get; set; } = [];
}

public class TicketOptionDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Code { get; set; }
}

public class TicketTransitionAvailabilityDto
{
    public Guid TicketId { get; set; }
    public string CurrentStatusCode { get; set; } = string.Empty;
    public string? NextStatusCode { get; set; }
    public string? NextStatusLabel { get; set; }
    public bool CanAdvance { get; set; }
    public IReadOnlyList<string> BlockingReasons { get; set; } = [];
}

public class TicketScheduledWorkListItemDto
{
    public Guid ScheduledWorkId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid WorkStatusId { get; set; }
    public string WorkStatusCode { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int WorkLogCount { get; set; }
}
