using App.BLL.DTO.Contacts;
using App.BLL.DTO.Tickets.Models;

namespace App.BLL.DTO.Residents.Models;

public class ResidentContactAssignmentModel
{
    public Guid ResidentContactId { get; init; }
    public Guid ResidentId { get; init; }
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
    public DateTime CreatedAt { get; init; }
}

public class ResidentContactListModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid ResidentId { get; init; }
    public string ResidentIdCode { get; init; } = default!;
    public string ResidentName { get; init; } = default!;
    public IReadOnlyList<ResidentContactAssignmentModel> Contacts { get; init; } = Array.Empty<ResidentContactAssignmentModel>();
    public IReadOnlyList<ContactBllDto> ExistingContacts { get; init; } = Array.Empty<ContactBllDto>();
    public IReadOnlyList<TicketOptionModel> ContactTypes { get; init; } = Array.Empty<TicketOptionModel>();
}
