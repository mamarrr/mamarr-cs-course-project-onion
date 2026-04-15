using System.Security.Claims;
using WebApp.ViewModels.Shared.Layout;

namespace WebApp.Services.SharedLayout;

public interface IWorkspaceLayoutContextProvider
{
    Task<WorkspaceLayoutContextViewModel> BuildAsync(
        ClaimsPrincipal user,
        WorkspaceLayoutRequestViewModel request,
        CancellationToken cancellationToken = default);
}
