namespace App.BLL.Contracts.Vendors.Commands;

public class CreateManagementVendorCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public string Culture { get; init; } = default!;
    public bool IsActive { get; init; }
}

public class UpdateManagementVendorCommand : CreateManagementVendorCommand
{
    public Guid VendorId { get; init; }
}

public class AddVendorCategoryCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid VendorId { get; init; }
    public Guid TicketCategoryId { get; init; }
}

public class AddVendorContactCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid VendorId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactValue { get; init; } = default!;
    public string? ContactNotes { get; init; }
    public string? FullName { get; init; }
    public string? RoleTitle { get; init; }
    public string Culture { get; init; } = default!;
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public bool Confirmed { get; init; }
    public bool IsPrimary { get; init; }
}

public class AssignVendorTicketCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid VendorId { get; init; }
    public Guid TicketId { get; init; }
}

public class AddVendorScheduledWorkCommand
{
    public Guid UserId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public Guid VendorId { get; init; }
    public Guid TicketId { get; init; }
    public Guid WorkStatusId { get; init; }
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public string? Notes { get; init; }
    public string Culture { get; init; } = default!;
}
