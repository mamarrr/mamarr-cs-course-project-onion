namespace App.DAL.Contracts.DAL.Vendors;

public class VendorListFilterDalDto
{
    public string? Search { get; init; }
    public bool IncludeInactive { get; init; }
    public Guid? TicketCategoryId { get; init; }
}

public class VendorListItemDalDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public bool IsActive { get; init; }
    public int ActiveCategoryCount { get; init; }
    public int AssignedTicketCount { get; init; }
    public int ContactCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class VendorEditDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public bool IsActive { get; init; }
}

public class VendorDetailsDalDto : VendorEditDalDto
{
    public IReadOnlyList<VendorCategoryDalDto> Categories { get; init; } = Array.Empty<VendorCategoryDalDto>();
    public IReadOnlyList<VendorTicketDalDto> Tickets { get; init; } = Array.Empty<VendorTicketDalDto>();
    public IReadOnlyList<VendorContactDalDto> Contacts { get; init; } = Array.Empty<VendorContactDalDto>();
    public IReadOnlyList<VendorScheduledWorkDalDto> ScheduledWorks { get; init; } = Array.Empty<VendorScheduledWorkDalDto>();
}

public class VendorCategoryDalDto
{
    public Guid LinkId { get; init; }
    public Guid TicketCategoryId { get; init; }
    public string Code { get; init; } = default!;
    public string Label { get; init; } = default!;
    public bool IsActive { get; init; }
}

public class VendorTicketDalDto
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
    public string StatusLabel { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public DateTime? DueAt { get; init; }
}

public class VendorContactDalDto
{
    public Guid VendorContactId { get; init; }
    public string ContactTypeLabel { get; init; } = default!;
    public string ContactValue { get; init; } = default!;
    public string? FullName { get; init; }
    public string? RoleTitle { get; init; }
    public bool IsPrimary { get; init; }
    public bool Confirmed { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
}

public class VendorScheduledWorkDalDto
{
    public Guid ScheduledWorkId { get; init; }
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string TicketTitle { get; init; } = default!;
    public string WorkStatusLabel { get; init; } = default!;
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public string? Notes { get; init; }
}

public class VendorOptionDalDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = default!;
    public string? Code { get; init; }
}

public class VendorTicketForAssignmentDalDto
{
    public Guid TicketId { get; init; }
    public Guid TicketCategoryId { get; init; }
    public string TicketNr { get; init; } = default!;
    public string Title { get; init; } = default!;
    public string StatusCode { get; init; } = default!;
}

public class VendorCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public string Culture { get; init; } = default!;
    public bool IsActive { get; init; }
}

public class VendorUpdateDalDto : VendorCreateDalDto
{
    public Guid Id { get; init; }
}

public class VendorContactCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
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

public class VendorScheduledWorkCreateDalDto
{
    public Guid ManagementCompanyId { get; init; }
    public Guid VendorId { get; init; }
    public Guid TicketId { get; init; }
    public Guid WorkStatusId { get; init; }
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public string? Notes { get; init; }
    public string Culture { get; init; } = default!;
    public Guid? ScheduledTicketStatusId { get; init; }
    public IReadOnlyCollection<string> ScheduledTransitionSourceStatusCodes { get; init; } = Array.Empty<string>();
}
