using System.Security.Claims;
using WebApp.ViewModels.Management.Layout;

namespace WebApp.Services.ManagementLayout;

public interface IManagementLayoutViewModelProvider
{
    Task<ManagementLayoutViewModel> BuildAsync(
        ClaimsPrincipal user,
        string currentController,
        string companySlug,
        string currentPathAndQuery,
        string currentUiCultureName,
        CancellationToken cancellationToken = default);
}
