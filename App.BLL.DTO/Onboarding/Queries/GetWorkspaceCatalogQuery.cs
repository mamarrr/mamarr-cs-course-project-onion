namespace App.BLL.DTO.Onboarding.Queries;

public class GetWorkspaceCatalogQuery
{
    public Guid AppUserId { get; init; }
    public string CompanySlug { get; init; } = string.Empty;
}
