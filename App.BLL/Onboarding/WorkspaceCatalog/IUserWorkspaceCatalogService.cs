namespace App.BLL.Onboarding.WorkspaceCatalog;

public interface IUserWorkspaceCatalogService
{
    Task<UserWorkspaceCatalogResult> GetUserContextCatalogAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}
