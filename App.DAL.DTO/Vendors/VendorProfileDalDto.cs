namespace App.DAL.DTO.Vendors;

public class VendorProfileDalDto
{
    public Guid Id { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string RegistryCode { get; init; } = default!;
    public string Notes { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public int ActiveCategoryCount { get; init; }
    public int AssignedTicketCount { get; init; }
    public int ContactCount { get; init; }
    public int ScheduledWorkCount { get; init; }
}
