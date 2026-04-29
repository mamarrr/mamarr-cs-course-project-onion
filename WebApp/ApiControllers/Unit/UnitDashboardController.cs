using System.Net;
using App.BLL.Contracts.Units.Services;
using App.DTO.v1;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Units;

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}/dashboard")]
public class UnitDashboardController : ProfileApiControllerBase
{
    private readonly IUnitWorkspaceService _unitWorkspaceService;
    private readonly UnitApiMapper _unitMapper;

    public UnitDashboardController(
        IUnitWorkspaceService unitWorkspaceService,
        UnitApiMapper unitMapper)
    {
        _unitWorkspaceService = unitWorkspaceService;
        _unitMapper = unitMapper;
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
        var result = await _unitWorkspaceService.GetDashboardAsync(
            _unitMapper.ToDashboardQuery(companySlug, customerSlug, propertySlug, unitSlug, User),
            cancellationToken);

        return result.ToActionResult(_unitMapper.ToDashboardResponseDto);
    }
}
