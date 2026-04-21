using System.Net;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Residents;
using App.DTO.v1;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;

namespace WebApp.ApiControllers.Resident;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/re/{residentIdCode}/dashboard")]
public class ResidentDashboardController : ProfileApiControllerBase
{
    private readonly IResidentAccessService _residentAccessService;

    public ResidentDashboardController(IResidentAccessService residentAccessService)
    {
        _residentAccessService = residentAccessService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ResidentDashboardResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentDashboardResponseDto>> GetDashboard(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var context = access.Context!;
        return Ok(new ResidentDashboardResponseDto
        {
            Dashboard = CreateDashboard(
                "Resident dashboard",
                "Dashboard",
                new ApiRouteContextDto
                {
                    CompanySlug = context.CompanySlug,
                    CompanyName = context.CompanyName,
                    ResidentIdCode = context.ResidentIdCode,
                    ResidentDisplayName = context.FullName,
                    CurrentSection = "resident-dashboard"
                })
        });
    }

    private async Task<(ResidentDashboardContext? Context, ActionResult? ErrorResult)> ResolveDashboardAccessAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var access = await _residentAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            residentIdCode,
            cancellationToken);

        if (access.CompanyNotFound || access.ResidentNotFound)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Resident context was not found.", ApiErrorCodes.NotFound)));
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (access.Context, null);
    }
}
