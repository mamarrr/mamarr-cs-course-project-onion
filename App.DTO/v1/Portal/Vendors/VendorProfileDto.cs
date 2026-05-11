namespace App.DTO.v1.Portal.Vendors;

public class VendorProfileDto
{
    public Guid Id { get; set; }
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ActiveCategoryCount { get; set; }
    public int AssignedTicketCount { get; set; }
    public int ContactCount { get; set; }
    public int ScheduledWorkCount { get; set; }
    public string Path { get; set; } = string.Empty;
}
