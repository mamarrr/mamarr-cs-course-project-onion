using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Residents;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.Residents;
using App.DTO.v1.Portal.Residents;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/residents")]
public class ResidentsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CreateResidentDto, ResidentBllDto> _createMapper;
    private readonly IBaseMapper<UpdateResidentProfileDto, ResidentBllDto> _updateMapper;
    private readonly ResidentListItemApiMapper _listItemMapper;
    private readonly ResidentProfileApiMapper _profileMapper;

    public ResidentsController(IAppBLL bll)
    {
        _bll = bll;

        var commandMapper = new ResidentApiMapper();
        _createMapper = commandMapper;
        _updateMapper = commandMapper;
        _listItemMapper = new ResidentListItemApiMapper();
        _profileMapper = new ResidentProfileApiMapper();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResidentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResidentListItemDto>>> List(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Residents.ListForCompanyAsync(
            ToCompanyRoute(companySlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var residents = result.Value.Residents
            .Select(resident => _listItemMapper.Map(resident, result.Value.CompanySlug))
            .ToList();

        return Ok(residents);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResidentProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResidentProfileDto>> Create(
        string companySlug,
        CreateResidentDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _createMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Resident payload is required.");
        }

        var result = await _bll.Residents.CreateAndGetProfileAsync(
            ToCompanyRoute(companySlug, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var profile = _profileMapper.Map(result.Value);
        return Created(profile.Path, profile);
    }

    [HttpGet("{residentIdCode}/profile")]
    [ProducesResponseType(typeof(ResidentProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentProfileDto>> Profile(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Residents.GetProfileAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_profileMapper.Map(result.Value));
    }

    [HttpPut("{residentIdCode}/profile")]
    [ProducesResponseType(typeof(ResidentProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentProfileDto>> UpdateProfile(
        string companySlug,
        string residentIdCode,
        UpdateResidentProfileDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Resident profile payload is required.");
        }

        var result = await _bll.Residents.UpdateAndGetProfileAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_profileMapper.Map(result.Value));
    }

    [HttpDelete("{residentIdCode}/profile")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> DeleteProfile(
        string companySlug,
        string residentIdCode,
        DeleteResidentDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Residents.DeleteAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            dto?.DeleteConfirmation ?? string.Empty,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Resident deleted."
        });
    }

    private static ManagementCompanyRoute ToCompanyRoute(string companySlug, Guid appUserId)
    {
        return new ManagementCompanyRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug
        };
    }

    private static ResidentRoute ToResidentRoute(
        string companySlug,
        string residentIdCode,
        Guid appUserId)
    {
        return new ResidentRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode
        };
    }
}
