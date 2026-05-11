using App.BLL.Contracts;
using App.BLL.DTO.Workspace.Queries;
using App.DTO.v1.Mappers.Workspace;
using App.DTO.v1.Workspace;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Workspace;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/workspaces")]
public class WorkspacesController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly WorkspaceApiMapper _mapper;

    public WorkspacesController(
        IAppBLL bll,
        WorkspaceApiMapper mapper)
    {
        _bll = bll;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(WorkspaceCatalogDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceCatalogDto>> GetCatalog(CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Workspaces.GetUserCatalogAsync(appUserId.Value, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("default-redirect")]
    [ProducesResponseType(typeof(WorkspaceRedirectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<WorkspaceRedirectDto>> DefaultRedirect(CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Workspaces.ResolveWorkspaceEntryPointAsync(
            new ResolveWorkspaceEntryPointQuery
            {
                AppUserId = appUserId.Value
            },
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return result.Value is null
            ? NoContent()
            : Ok(_mapper.Map(result.Value));
    }

    [HttpPost("select")]
    [ProducesResponseType(typeof(WorkspaceRedirectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceRedirectDto>> Select(
        SelectWorkspaceDto dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        if (dto.ContextId == Guid.Empty)
        {
            return InvalidRequest("Context id is required.");
        }

        var result = await _bll.Workspaces.AuthorizeContextSelectionAsync(
            new AuthorizeContextSelectionQuery
            {
                AppUserId = appUserId.Value,
                ContextType = dto.ContextType,
                ContextId = dto.ContextId
            },
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        if (!result.Value.Authorized)
        {
            return ForbiddenRequest("Workspace selection is not authorized.");
        }

        return Ok(_mapper.Map(result.Value));
    }
}
