using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.DTO.v1.Mappers.Portal.Dashboards;
using App.DTO.v1.Portal.Dashboards;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}")]
public class DashboardsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly PortalDashboardApiMapper _mapper = new();

    public DashboardsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ManagementDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagementDashboardDto>> ManagementDashboard(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.PortalDashboards.GetManagementDashboardAsync(
            new ManagementCompanyRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug
            },
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/dashboard")]
    [ProducesResponseType(typeof(CustomerDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDashboardDto>> CustomerDashboard(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.PortalDashboards.GetCustomerDashboardAsync(
            new CustomerRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug
            },
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/dashboard")]
    [ProducesResponseType(typeof(PropertyDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PropertyDashboardDto>> PropertyDashboard(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.PortalDashboards.GetPropertyDashboardAsync(
            new PropertyRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug
            },
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/dashboard")]
    [ProducesResponseType(typeof(UnitDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnitDashboardDto>> UnitDashboard(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.PortalDashboards.GetUnitDashboardAsync(
            new UnitRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug,
                UnitSlug = unitSlug
            },
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("residents/{residentIdCode}/dashboard")]
    [ProducesResponseType(typeof(ResidentDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentDashboardDto>> ResidentDashboard(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.PortalDashboards.GetResidentDashboardAsync(
            new ResidentRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                ResidentIdCode = residentIdCode
            },
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }
}
