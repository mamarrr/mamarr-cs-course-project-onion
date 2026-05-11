using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Properties;
using App.DTO.v1.Mappers.Portal.Properties;
using App.DTO.v1.Portal.Properties;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/customers/{customerSlug}/properties")]
public class PropertiesController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CreatePropertyDto, PropertyBllDto> _createMapper = new PropertyApiMapper();
    private readonly IBaseMapper<UpdatePropertyProfileDto, PropertyBllDto> _updateMapper = new PropertyApiMapper();
    private readonly PropertyListItemApiMapper _listItemMapper = new();
    private readonly PropertyProfileApiMapper _profileMapper = new();

    public PropertiesController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PropertyListItemDto>>> List(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Properties.ListForCustomerAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId.Value),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(result.Value.Select(_listItemMapper.Map).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(PropertyProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<PropertyProfileDto>> Create(
        string companySlug,
        string customerSlug,
        CreatePropertyDto dto,
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
            return InvalidRequest("Property payload is required.");
        }

        var result = await _bll.Properties.CreateAndGetProfileAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId.Value),
            bllDto,
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return CreatedAtAction(
            nameof(GetProfile),
            new
            {
                version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1",
                companySlug,
                customerSlug,
                propertySlug = result.Value.PropertySlug
            },
            _profileMapper.Map(result.Value));
    }

    [HttpGet("{propertySlug}/profile")]
    [ProducesResponseType(typeof(PropertyProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PropertyProfileDto>> GetProfile(
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

        var result = await _bll.Properties.GetProfileAsync(
            ToPropertyRoute(companySlug, customerSlug, propertySlug, appUserId.Value),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_profileMapper.Map(result.Value));
    }

    [HttpPut("{propertySlug}/profile")]
    [ProducesResponseType(typeof(PropertyProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PropertyProfileDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        UpdatePropertyProfileDto dto,
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
            return InvalidRequest("Property payload is required.");
        }

        var result = await _bll.Properties.UpdateAndGetProfileAsync(
            ToPropertyRoute(companySlug, customerSlug, propertySlug, appUserId.Value),
            bllDto,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_profileMapper.Map(result.Value));
    }

    [HttpDelete("{propertySlug}/profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        DeleteConfirmationDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Properties.DeleteAsync(
            ToPropertyRoute(companySlug, customerSlug, propertySlug, appUserId.Value),
            dto?.DeleteConfirmation ?? string.Empty,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : NoContent();
    }

    private static CustomerRoute ToCustomerRoute(string companySlug, string customerSlug, Guid appUserId)
    {
        return new CustomerRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug
        };
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
}
