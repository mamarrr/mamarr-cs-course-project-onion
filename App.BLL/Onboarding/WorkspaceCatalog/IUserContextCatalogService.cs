namespace App.BLL.Onboarding.WorkspaceCatalog;

public interface IUserContextCatalogService
{
    Task<UserContextCatalogResult> GetUserContextCatalogAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}
