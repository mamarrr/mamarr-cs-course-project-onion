using System.Net;
using App.BLL.Contracts.Properties.Services;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Workspace;
using App.DTO.v1;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Properties;

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}/dashboard")]
public class UnitDashboardController : ProfileApiControllerBase
{
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly PropertyApiMapper _propertyMapper;

    public UnitDashboardController(
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        PropertyApiMapper propertyMapper)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _propertyMapper = propertyMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<UnitDashboardResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitDashboardResponseDto>> GetDashboard(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var context = access.Context!;
        return Ok(new UnitDashboardResponseDto
        {
            Dashboard = CreateDashboard(
                "Unit dashboard",
                "Dashboard",
                new ApiRouteContextDto
                {
                    CompanySlug = context.CompanySlug,
                    CompanyName = context.CompanyName,
                    CustomerSlug = context.CustomerSlug,
                    CustomerName = context.CustomerName,
                    PropertySlug = context.PropertySlug,
                    PropertyName = context.PropertyName,
                    UnitSlug = context.UnitSlug,
                    UnitName = context.UnitNr,
                    CurrentSection = "unit-dashboard"
                })
        });
    }

    private async Task<(UnitDashboardContext? Context, ActionResult? ErrorResult)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var propertyAccess = await _propertyWorkspaceService.GetWorkspaceAsync(
            _propertyMapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);
        if (propertyAccess.IsFailed)
        {
            return (null, propertyAccess.ToActionResult(_ => new UnitDashboardResponseDto()).Result);
        }

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Value,
            unitSlug,
            cancellationToken);

        if (unitAccess.UnitNotFound || unitAccess.Context == null)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Unit context was not found.", ApiErrorCodes.NotFound)));
        }

        if (!unitAccess.IsAuthorized)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (unitAccess.Context, null);
    }
}
