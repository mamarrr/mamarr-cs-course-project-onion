using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Vendors;
using App.DTO.v1.Mappers.Portal.VendorContacts;
using App.DTO.v1.Portal.VendorContacts;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/vendors/{vendorId:guid}/contacts")]
public class VendorContactsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<AttachExistingVendorContactDto, VendorContactBllDto> _attachMapper;
    private readonly IBaseMapper<CreateAndAttachVendorContactDto, VendorContactBllDto> _createAssignmentMapper;
    private readonly IBaseMapper<CreateAndAttachVendorContactDto, ContactBllDto> _createContactMapper;
    private readonly IBaseMapper<UpdateVendorContactDto, VendorContactBllDto> _updateMapper;
    private readonly VendorContactListApiMapper _listMapper = new();

    public VendorContactsController(IAppBLL bll)
    {
        _bll = bll;

        var commandMapper = new VendorContactApiMapper();
        _attachMapper = commandMapper;
        _createAssignmentMapper = commandMapper;
        _createContactMapper = commandMapper;
        _updateMapper = commandMapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> List(
        string companySlug,
        Guid vendorId,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Vendors.ListContactsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_listMapper.Map(result.Value));
    }

    [HttpGet("{vendorContactId:guid}")]
    [ProducesResponseType(typeof(VendorContactEditModelDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactEditModelDto>> Get(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Vendors.ListContactsAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var model = _listMapper.MapEditModel(result.Value, vendorContactId);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpPost("attach")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> AttachExisting(
        string companySlug,
        Guid vendorId,
        AttachExistingVendorContactDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _attachMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Vendor contact payload is required.");
        }

        var result = await _bll.Vendors.AddContactAsync(route, bllDto, null, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_listMapper.Map(result.Value));
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> CreateAndAttach(
        string companySlug,
        Guid vendorId,
        CreateAndAttachVendorContactDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorRoute(companySlug, vendorId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var assignmentDto = _createAssignmentMapper.Map(dto);
        var contactDto = _createContactMapper.Map(dto);
        if (assignmentDto is null || contactDto is null)
        {
            return InvalidRequest("Vendor contact payload is required.");
        }

        var result = await _bll.Vendors.AddContactAsync(route, assignmentDto, contactDto, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_listMapper.Map(result.Value));
    }

    [HttpPut("{vendorContactId:guid}")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> Update(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        UpdateVendorContactDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Vendor contact payload is required.");
        }

        var result = await _bll.Vendors.UpdateContactAsync(route, bllDto, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_listMapper.Map(result.Value));
    }

    [HttpDelete("{vendorContactId:guid}")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> Delete(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunCommandAndGetListAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.RemoveContactAsync(route, cancellationToken),
            cancellationToken);
    }

    [HttpPost("{vendorContactId:guid}/set-primary")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> SetPrimary(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunCommandAndGetListAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.SetPrimaryContactAsync(route, cancellationToken),
            cancellationToken);
    }

    [HttpPost("{vendorContactId:guid}/confirm")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> Confirm(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunCommandAndGetListAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.ConfirmContactAsync(route, cancellationToken),
            cancellationToken);
    }

    [HttpPost("{vendorContactId:guid}/unconfirm")]
    [ProducesResponseType(typeof(VendorContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VendorContactListDto>> Unconfirm(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        CancellationToken cancellationToken)
    {
        return await RunCommandAndGetListAsync(
            companySlug,
            vendorId,
            vendorContactId,
            route => _bll.Vendors.UnconfirmContactAsync(route, cancellationToken),
            cancellationToken);
    }

    private async Task<ActionResult<VendorContactListDto>> RunCommandAndGetListAsync(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId,
        Func<VendorContactRoute, Task<FluentResults.Result>> command,
        CancellationToken cancellationToken)
    {
        var route = ToVendorContactRoute(companySlug, vendorId, vendorContactId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var commandResult = await command(route);
        if (commandResult.IsFailed)
        {
            return ToApiError(commandResult.Errors);
        }

        var listResult = await _bll.Vendors.ListContactsAsync(
            ToVendorRoute(companySlug, vendorId, route.AppUserId)!,
            cancellationToken);

        return listResult.IsFailed ? ToApiError(listResult.Errors) : Ok(_listMapper.Map(listResult.Value));
    }

    private VendorRoute? ToVendorRoute(string companySlug, Guid vendorId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : ToVendorRoute(companySlug, vendorId, appUserId.Value);
    }

    private static VendorRoute ToVendorRoute(string companySlug, Guid vendorId, Guid appUserId)
    {
        return new VendorRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            VendorId = vendorId
        };
    }

    private VendorContactRoute? ToVendorContactRoute(
        string companySlug,
        Guid vendorId,
        Guid vendorContactId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new VendorContactRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                VendorId = vendorId,
                VendorContactId = vendorContactId
            };
    }
}
