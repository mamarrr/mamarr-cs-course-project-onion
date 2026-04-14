namespace App.BLL.Onboarding;

public interface IUserContextCatalogService
{
    Task<UserContextCatalogResult> GetUserContextCatalogAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}
