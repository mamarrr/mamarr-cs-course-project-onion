using App.DTO.v1.Shared;

namespace App.DTO.v1.Portal.Vendors;

public class VendorCategoryAssignmentListDto
{
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public IReadOnlyList<VendorCategoryAssignmentDto> Assignments { get; set; } = [];
    public IReadOnlyList<LookupOptionDto> AvailableCategories { get; set; } = [];
}
