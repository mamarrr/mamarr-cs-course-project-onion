using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.DTO.v1.Mappers.Portal.Companies;
using App.DTO.v1.Portal.Companies;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}")]
public class ManagementCompaniesController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly ManagementCompanyApiMapper _mapper = new();

    public ManagementCompaniesController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ManagementCompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagementCompanyProfileDto>> GetProfile(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.ManagementCompanies.GetProfileAsync(
            ToRoute(appUserId.Value, companySlug),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpPut]
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ManagementCompanyProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagementCompanyProfileDto>> UpdateProfile(
        string companySlug,
        UpdateManagementCompanyDto dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        if (dto is null)
        {
            return InvalidRequest("Management company payload is required.");
        }

        var result = await _bll.ManagementCompanies.UpdateAndGetProfileAsync(
            ToRoute(appUserId.Value, companySlug),
            _mapper.Map(dto),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_mapper.Map(result.Value));
    }

    [HttpDelete]
    [HttpDelete("profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProfile(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.ManagementCompanies.DeleteAsync(
            ToRoute(appUserId.Value, companySlug),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : NoContent();
    }

    private static ManagementCompanyRoute ToRoute(Guid appUserId, string companySlug)
    {
        return new ManagementCompanyRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug
        };
    }
}
