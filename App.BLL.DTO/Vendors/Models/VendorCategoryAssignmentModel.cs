using App.BLL.DTO.Tickets.Models;

namespace App.BLL.DTO.Vendors.Models;

public class VendorCategoryAssignmentModel
{
    public Guid AssignmentId { get; init; }
    public Guid VendorId { get; init; }
    public Guid TicketCategoryId { get; init; }
    public string CategoryCode { get; init; } = default!;
    public string CategoryLabel { get; init; } = default!;
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class VendorCategoryAssignmentListModel
{
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = default!;
    public IReadOnlyList<VendorCategoryAssignmentModel> Assignments { get; init; } = Array.Empty<VendorCategoryAssignmentModel>();
    public IReadOnlyList<TicketOptionModel> AvailableCategories { get; init; } = Array.Empty<TicketOptionModel>();
}

