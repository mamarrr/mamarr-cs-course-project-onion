namespace App.BLL.DTO.Residents.Models;

public class ResidentProfileModel
{
    public Guid ResidentId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public string ResidentIdCode { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string? PreferredLanguage { get; init; }
    
}
