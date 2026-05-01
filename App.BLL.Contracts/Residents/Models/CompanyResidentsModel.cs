namespace App.BLL.Contracts.Residents.Models;

public class CompanyResidentsModel
{
    public Guid AppUserId { get; init; }
    public Guid ManagementCompanyId { get; init; }
    public string CompanySlug { get; init; } = default!;
    public string CompanyName { get; init; } = default!;
    public IReadOnlyList<ResidentListItemModel> Residents { get; init; } = Array.Empty<ResidentListItemModel>();
}
