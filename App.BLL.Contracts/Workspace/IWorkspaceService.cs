using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Workspace.Models;
using App.BLL.DTO.Workspace.Queries;
using FluentResults;

namespace App.BLL.Contracts.Workspace;

public interface IWorkspaceService
{
    Task<Result<bool>> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result<string?>> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UserHasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkspaceCatalogModel>> GetCatalogAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<WorkspaceRedirectModel?>> ResolveContextRedirectAsync(
        ResolveWorkspaceRedirectQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<WorkspaceSelectionAuthorizationModel>> AuthorizeContextSelectionAsync(
        AuthorizeContextSelectionQuery query,
        CancellationToken cancellationToken = default);
}
