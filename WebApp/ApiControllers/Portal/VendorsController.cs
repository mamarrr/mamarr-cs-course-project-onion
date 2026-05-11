using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Vendors;
using App.DTO.v1.Mappers.Portal.Vendors;
using App.DTO.v1.Portal.Vendors;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/vendors")]
public class VendorsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<VendorRequestDto, VendorBllDto> _requestMapper = new VendorApiMapper();
    private readonly IBaseMapper<AssignVendorCategoryDto, VendorTicketCategoryBllDto> _assignCategoryMapper =
        new VendorCategoryApiMapper();
    private readonly IBaseMapper<UpdateVendorCategoryDto, VendorTicketCategoryBllDto> _updateCategoryMapper =
        new VendorCategoryApiMapper();
    private readonly VendorListItemApiMapper _listItemMapper = new();
    private readonly VendorProfileApiMapper _profileMapper = new();
    private readonly VendorCategoryApiMapper _categoryMapper = new();

    public VendorsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VendorListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VendorListItemDto>>> List(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var route = ToCompanyRoute(companySlug);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Vendors.ListForCompanyAsync(route, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(result.Value.Select(_listItemMapper.Map).ToList());
    }

    [HttpPost]
    [ProducesResponseType(typeof(VendorProfileDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<VendorProfileDto>> Create(
        string companySlug,
        VendorRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToCompanyRoute(companySlug);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _requestMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Vendor payload is required.");
        }

        var result = await _bll.Vendors.CreateAndGetProfileAsync(route, bllDto, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var response = _profileMapper.Map(result.Value);
        return CreatedAtAction(
            nameof(GetProfile),
            new
            {
                version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1",
                companySlug,
                vendorId = result.Value.Id
            },
            response);
    }

    [HttpGet("{vendorId:guid}")]
    [ProducesResponseType(typeof(VendorProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorProfileDto>> GetProfile(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Vendors.GetProfileAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_profileMapper.Map(result.Value));
    }

    [HttpPut("{vendorId:guid}")]
    [ProducesResponseType(typeof(VendorProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorProfileDto>> Update(
        string companySlug,
        Guid vendorId,
        VendorRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _requestMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Vendor payload is required.");
        }

        var result = await _bll.Vendors.UpdateAndGetProfileAsync(route, bllDto, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_profileMapper.Map(result.Value));
    }

    [HttpDelete("{vendorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        string companySlug,
        Guid vendorId,
        DeleteVendorDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        if (dto is null)
        {
            return InvalidRequest("Vendor delete payload is required.");
        }

        var result = await _bll.Vendors.DeleteAsync(route, dto.ConfirmationRegistryCode, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : NoContent();
    }

    [HttpGet("{vendorId:guid}/categories")]
    [ProducesResponseType(typeof(VendorCategoryAssignmentListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorCategoryAssignmentListDto>> ListCategories(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Vendors.ListCategoryAssignmentsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_categoryMapper.Map(result.Value));
    }

    [HttpPost("{vendorId:guid}/categories")]
    [ProducesResponseType(typeof(VendorCategoryAssignmentListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorCategoryAssignmentListDto>> AssignCategory(
        string companySlug,
        Guid vendorId,
        AssignVendorCategoryDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _assignCategoryMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Vendor category payload is required.");
        }

        var result = await _bll.Vendors.AssignCategoryAsync(route, bllDto, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_categoryMapper.Map(result.Value));
    }

    [HttpPut("{vendorId:guid}/categories/{ticketCategoryId:guid}")]
    [ProducesResponseType(typeof(VendorCategoryAssignmentListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorCategoryAssignmentListDto>> UpdateCategory(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId,
        UpdateVendorCategoryDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorCategoryRoute(companySlug, vendorId, ticketCategoryId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateCategoryMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Vendor category payload is required.");
        }

        var result = await _bll.Vendors.UpdateCategoryAssignmentAsync(route, bllDto, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_categoryMapper.Map(result.Value));
    }

    [HttpDelete("{vendorId:guid}/categories/{ticketCategoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveCategory(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken)
    {
        var route = ToVendorCategoryRoute(companySlug, vendorId, ticketCategoryId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Vendors.RemoveCategoryAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : NoContent();
    }

    private ManagementCompanyRoute? ToCompanyRoute(string companySlug)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new ManagementCompanyRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug
            };
    }

    private VendorRoute? ToVendorRoute(string companySlug, Guid vendorId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new VendorRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                VendorId = vendorId
            };
    }

    private VendorCategoryRoute? ToVendorCategoryRoute(
        string companySlug,
        Guid vendorId,
        Guid ticketCategoryId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new VendorCategoryRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                VendorId = vendorId,
                TicketCategoryId = ticketCategoryId
            };
    }
}
