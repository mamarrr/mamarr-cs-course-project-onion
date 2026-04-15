using System.Security.Claims;
using WebApp.ViewModels.Management.Layout;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Services.ManagementLayout;

public interface IManagementLayoutViewModelProvider
{
    Task<ManagementLayoutViewModel> BuildAsync(
        ClaimsPrincipal user,
        ManagementLayoutRequestViewModel request,
        CancellationToken cancellationToken = default);
}
