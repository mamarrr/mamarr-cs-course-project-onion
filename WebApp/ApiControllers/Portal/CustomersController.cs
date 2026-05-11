using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers;
using App.DTO.v1.Mappers.Portal.Customers;
using App.DTO.v1.Portal.Customers;
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
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/customers")]
public class CustomersController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CustomerRequestDto, CustomerBllDto> _requestMapper = new CustomerApiMapper();
    private readonly CustomerListItemApiMapper _listItemMapper = new();
    private readonly CustomerProfileApiMapper _profileMapper = new();

    public CustomersController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomerListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerListItemDto>>> List(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Customers.ListForCompanyAsync(
            ToCompanyRoute(companySlug, appUserId.Value),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(result.Value.Select(_listItemMapper.Map).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerProfileDto>> Create(
        string companySlug,
        CustomerRequestDto dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _requestMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Customer payload is required.");
        }

        var result = await _bll.Customers.CreateAndGetProfileAsync(
            ToCompanyRoute(companySlug, appUserId.Value),
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
                customerSlug = result.Value.Slug
            },
            _profileMapper.Map(result.Value));
    }

    [HttpGet("{customerSlug}/profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileDto>> GetProfile(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Customers.GetProfileAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId.Value),
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_profileMapper.Map(result.Value));
    }

    [HttpPut("{customerSlug}/profile")]
    [ProducesResponseType(typeof(CustomerProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerProfileDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        CustomerRequestDto dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _requestMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Customer payload is required.");
        }

        var result = await _bll.Customers.UpdateAndGetProfileAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId.Value),
            bllDto,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_profileMapper.Map(result.Value));
    }

    [HttpDelete("{customerSlug}/profile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProfile(
        string companySlug,
        string customerSlug,
        DeleteConfirmationDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Customers.DeleteAsync(
            ToCustomerRoute(companySlug, customerSlug, appUserId.Value),
            dto?.DeleteConfirmation ?? string.Empty,
            cancellationToken);

        return result.IsFailed
            ? ToApiError(result.Errors)
            : NoContent();
    }

    private static ManagementCompanyRoute ToCompanyRoute(string companySlug, Guid appUserId)
    {
        return new ManagementCompanyRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug
        };
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
}
