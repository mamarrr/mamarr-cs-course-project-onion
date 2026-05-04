namespace App.BLL.Contracts.Vendors.Models;

public class ManagementVendorsModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public VendorFilterModel Filter { get; init; } = new();
    public IReadOnlyList<VendorListItemModel> Vendors { get; init; } = Array.Empty<VendorListItemModel>();
    public VendorOptionsModel Options { get; init; } = new();
}

public class VendorFilterModel
{
    public string? Search { get; init; }
    public bool IncludeInactive { get; init; }
    public Guid? TicketCategoryId { get; init; }
}

public class VendorListItemModel
{
    public Guid VendorId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public bool IsActive { get; init; }
    public int ActiveCategoryCount { get; init; }
    public int AssignedTicketCount { get; init; }
    public int ContactCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ManagementVendorDetailsModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid VendorId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public bool IsActive { get; init; }
    public IReadOnlyList<VendorCategoryModel> Categories { get; init; } = Array.Empty<VendorCategoryModel>();
    public IReadOnlyList<VendorTicketModel> Tickets { get; init; } = Array.Empty<VendorTicketModel>();
    public IReadOnlyList<VendorContactModel> Contacts { get; init; } = Array.Empty<VendorContactModel>();
    public IReadOnlyList<VendorScheduledWorkModel> ScheduledWorks { get; init; } = Array.Empty<VendorScheduledWorkModel>();
    public IReadOnlyList<VendorTicketOptionModel> AssignableTickets { get; init; } = Array.Empty<VendorTicketOptionModel>();
    public VendorOptionsModel Options { get; init; } = new();
}

public class VendorCategoryModel
{
    public Guid TicketCategoryId { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
    public bool IsActive { get; init; }
}

public class VendorTicketModel
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public DateTime? DueAt { get; init; }
}

public class VendorContactModel
{
    public string ContactTypeLabel { get; init; } = default!;
    public string ContactValue { get; init; } = default!;
    public string? FullName { get; init; }
    public string? RoleTitle { get; init; }
    public bool IsPrimary { get; init; }
    public bool Confirmed { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

public class VendorScheduledWorkModel
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public string WorkStatusLabel { get; init; } = default!;
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public string? Notes { get; init; }
}

public class VendorOptionsModel
{
    public IReadOnlyList<VendorOptionModel> TicketCategories { get; init; } = Array.Empty<VendorOptionModel>();
    public IReadOnlyList<VendorOptionModel> ContactTypes { get; init; } = Array.Empty<VendorOptionModel>();
    public IReadOnlyList<VendorOptionModel> WorkStatuses { get; init; } = Array.Empty<VendorOptionModel>();
}

public class VendorOptionModel
{
    public Guid Id { get; init; }
    public string Label { get; init; } = default!;
    public string? Code { get; init; }
}

public class VendorTicketOptionModel
{
    public Guid TicketId { get; init; }
    public string Label { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
}
