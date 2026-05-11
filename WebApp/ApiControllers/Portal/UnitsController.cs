using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Units;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.Units;
using App.DTO.v1.Portal.Units;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/customers/{customerSlug}/properties/{propertySlug}/units")]
public class UnitsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CreateUnitDto, UnitBllDto> _createMapper;
    private readonly IBaseMapper<UpdateUnitProfileDto, UnitBllDto> _updateMapper;
    private readonly UnitListItemApiMapper _listItemMapper;
    private readonly UnitProfileApiMapper _profileMapper;

    public UnitsController(IAppBLL bll)
    {
        _bll = bll;

        var commandMapper = new UnitApiMapper();
        _createMapper = commandMapper;
        _updateMapper = commandMapper;
        _listItemMapper = new UnitListItemApiMapper();
        _profileMapper = new UnitProfileApiMapper();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UnitListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UnitListItemDto>>> List(
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

        var result = await _bll.Units.ListForPropertyAsync(
            ToPropertyRoute(companySlug, customerSlug, propertySlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var units = result.Value.Units
            .Select(unit => _listItemMapper.Map(
                unit,
                result.Value.CompanySlug,
                result.Value.CustomerSlug,
                result.Value.PropertySlug))
            .ToList();

        return Ok(units);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UnitProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UnitProfileDto>> Create(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CreateUnitDto? dto,
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
            return InvalidRequest("Unit payload is required.");
        }

        var result = await _bll.Units.CreateAndGetProfileAsync(
            ToPropertyRoute(companySlug, customerSlug, propertySlug, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var profile = _profileMapper.Map(result.Value);
        return Created(profile.Path, profile);
    }

    [HttpGet("{unitSlug}/profile")]
    [ProducesResponseType(typeof(UnitProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnitProfileDto>> Profile(
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

        var result = await _bll.Units.GetProfileAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_profileMapper.Map(result.Value));
    }

    [HttpPut("{unitSlug}/profile")]
    [ProducesResponseType(typeof(UnitProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnitProfileDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        UpdateUnitProfileDto? dto,
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
            return InvalidRequest("Unit profile payload is required.");
        }

        var result = await _bll.Units.UpdateAndGetProfileAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_profileMapper.Map(result.Value));
    }

    [HttpDelete("{unitSlug}/profile")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> DeleteProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        DeleteUnitDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Units.DeleteAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            dto?.DeleteConfirmation ?? string.Empty,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Unit deleted."
        });
    }

    private static PropertyRoute ToPropertyRoute(
        string companySlug,
        string customerSlug,
        string propertySlug,
        Guid appUserId)
    {
        return new PropertyRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug
        };
    }

    private static UnitRoute ToUnitRoute(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid appUserId)
    {
        return new UnitRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug
        };
    }
}
