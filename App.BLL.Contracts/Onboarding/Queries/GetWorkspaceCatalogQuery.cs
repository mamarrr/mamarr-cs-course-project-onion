namespace App.BLL.Contracts.Onboarding.Queries;

public class GetWorkspaceCatalogQuery
{
    public Guid AppUserId { get; init; }
    public string CompanySlug { get; init; } = string.Empty;
}
