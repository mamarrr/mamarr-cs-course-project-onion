using App.BLL.DTO.Contacts;
using App.BLL.DTO.Tickets.Models;

namespace App.BLL.DTO.Vendors.Models;

public class VendorContactAssignmentModel
{
    public Guid VendorContactId { get; init; }
    public Guid VendorId { get; init; }
    public Guid ContactId { get; init; }
    public Guid ContactTypeId { get; init; }
    public string ContactTypeCode { get; init; } = default!;
    public string ContactTypeLabel { get; init; } = default!;
    public string ContactValue { get; init; } = default!;
    public string? ContactNotes { get; init; }
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public bool Confirmed { get; init; }
    public bool IsPrimary { get; init; }
    public string? FullName { get; init; }
    public string? RoleTitle { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class VendorContactListModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = default!;
    public IReadOnlyList<VendorContactAssignmentModel> Contacts { get; init; } = Array.Empty<VendorContactAssignmentModel>();
    public IReadOnlyList<ContactBllDto> ExistingContacts { get; init; } = Array.Empty<ContactBllDto>();
    public IReadOnlyList<TicketOptionModel> ContactTypes { get; init; } = Array.Empty<TicketOptionModel>();
}

