using System.Security.Claims;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Services.SharedLayout;

public interface IWorkspaceLayoutContextProvider
{
    Task<WorkspaceLayoutContextViewModel> BuildAsync(
        ClaimsPrincipal user,
        string currentController,
        string companySlug,
        string currentPathAndQuery,
        string currentUiCultureName,
        CancellationToken cancellationToken = default);
}
