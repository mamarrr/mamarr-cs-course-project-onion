namespace App.BLL.Contracts.Tickets.Commands;

public class CreateManagementTicketCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Culture { get; init; } = default!;
    public Guid TicketCategoryId { get; init; }
    public Guid TicketPriorityId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ResidentId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueAt { get; init; }
}

public class UpdateManagementTicketCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Culture { get; init; } = default!;
    public Guid TicketCategoryId { get; init; }
    public Guid TicketStatusId { get; init; }
    public Guid TicketPriorityId { get; init; }
    public Guid? CustomerId { get; init; }
    public Guid? PropertyId { get; init; }
    public Guid? UnitId { get; init; }
    public Guid? ResidentId { get; init; }
    public Guid? VendorId { get; init; }
    public DateTime? DueAt { get; init; }
}

public class DeleteManagementTicketCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid TicketId { get; init; }
}

public class AdvanceManagementTicketStatusCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid TicketId { get; init; }
}
