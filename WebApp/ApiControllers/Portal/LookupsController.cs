using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.DTO.v1.Mappers.Portal.Leases;
using App.DTO.v1.Mappers.Portal.Lookups;
using App.DTO.v1.Mappers.Portal.Tickets;
using App.DTO.v1.Portal.Leases;
using App.DTO.v1.Portal.Tickets;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal")]
public class LookupsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly LookupApiMapper _lookupMapper = new();
    private readonly LeaseResponseApiMapper _leaseMapper = new();
    private readonly TicketApiMapper _ticketMapper = new();

    public LookupsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("lookups/property-types")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LookupOptionDto>>> PropertyTypes(
        CancellationToken cancellationToken)
    {
        var result = await _bll.Properties.GetPropertyTypeOptionsAsync(cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_lookupMapper.MapPropertyTypes(result.Value));
    }

    [HttpGet("lookups/lease-roles")]
    [ProducesResponseType(typeof(LeaseRoleOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseRoleOptionsDto>> LeaseRoles(
        CancellationToken cancellationToken)
    {
        var result = await _bll.Leases.ListLeaseRolesAsync(cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_leaseMapper.Map(result.Value));
    }

    [HttpGet("companies/{companySlug}/lookups/ticket-options")]
    [ProducesResponseType(typeof(TicketSelectorOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketSelectorOptionsDto>> TicketOptions(
        string companySlug,
        [FromQuery] TicketSelectorOptionsQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToSelectorOptionsRoute(companySlug, query);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.GetSelectorOptionsAsync(
            route,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_ticketMapper.Map(result.Value));
    }

    private TicketSelectorOptionsRoute? ToSelectorOptionsRoute(
        string companySlug,
        TicketSelectorOptionsQueryDto query)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new TicketSelectorOptionsRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                CustomerId = query.CustomerId,
                PropertyId = query.PropertyId,
                UnitId = query.UnitId,
                CategoryId = query.CategoryId
            };
    }
}
