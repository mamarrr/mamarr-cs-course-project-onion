using System.Net;
using App.BLL.Contracts.Residents.Services;
using App.DTO.v1;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Residents;

namespace WebApp.ApiControllers.Resident;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/re/{residentIdCode}/dashboard")]
public class ResidentDashboardController : ProfileApiControllerBase
{
    private readonly IResidentWorkspaceService _residentWorkspaceService;
    private readonly ResidentApiMapper _residentMapper;

    public ResidentDashboardController(
        IResidentWorkspaceService residentWorkspaceService,
        ResidentApiMapper residentMapper)
    {
        _residentWorkspaceService = residentWorkspaceService;
        _residentMapper = residentMapper;
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
        var result = await _residentWorkspaceService.GetDashboardAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);

        return result.ToActionResult(_residentMapper.ToDashboardResponseDto);
    }
}
