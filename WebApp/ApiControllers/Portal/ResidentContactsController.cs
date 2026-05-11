using App.BLL.Contracts;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents;
using App.DTO.v1.Mappers.Portal.Contacts;
using App.DTO.v1.Portal.Contacts;
using Asp.Versioning;
using Base.Contracts;
using FluentResults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/residents/{residentIdCode}/contacts")]
public class ResidentContactsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<ResidentContactAssignmentDto, ResidentContactBllDto> _assignmentMapper;
    private readonly IBaseMapper<CreateAndAttachResidentContactDto, ResidentContactBllDto> _createAssignmentMapper;
    private readonly IBaseMapper<CreateAndAttachResidentContactDto, ContactBllDto> _createContactMapper;
    private readonly ResidentContactListApiMapper _listMapper = new();

    public ResidentContactsController(IAppBLL bll)
    {
        _bll = bll;

        var commandMapper = new ResidentContactApiMapper();
        _assignmentMapper = commandMapper;
        _createAssignmentMapper = commandMapper;
        _createContactMapper = commandMapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> List(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var route = ToResidentRoute(companySlug, residentIdCode);
        if (route.Result is not null)
        {
            return route.Result;
        }

        var result = await _bll.Residents.ListContactsAsync(route.Value!, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_listMapper.Map(result.Value));
    }

    [HttpGet("{residentContactId:guid}")]
    [ProducesResponseType(typeof(ResidentContactEditDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactEditDto>> GetEditModel(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        var route = ToResidentRoute(companySlug, residentIdCode);
        if (route.Result is not null)
        {
            return route.Result;
        }

        var result = await _bll.Residents.ListContactsAsync(route.Value!, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var contact = result.Value.Contacts.FirstOrDefault(item => item.ResidentContactId == residentContactId);
        if (contact is null)
        {
            return ToApiError(new[] { new NotFoundError("Resident contact was not found.") });
        }

        return Ok(_listMapper.MapEdit(result.Value, contact));
    }

    [HttpPost("attach")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> AttachExisting(
        string companySlug,
        string residentIdCode,
        ResidentContactAssignmentDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToResidentRoute(companySlug, residentIdCode);
        if (route.Result is not null)
        {
            return route.Result;
        }

        var bllDto = _assignmentMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Resident contact payload is required.");
        }

        var result = await _bll.Residents.AddContactAsync(route.Value!, bllDto, null, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_listMapper.Map(result.Value));
    }

    [HttpPost]
    [HttpPost("create")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResidentContactListDto>> CreateAndAttach(
        string companySlug,
        string residentIdCode,
        CreateAndAttachResidentContactDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToResidentRoute(companySlug, residentIdCode);
        if (route.Result is not null)
        {
            return route.Result;
        }

        var assignmentDto = _createAssignmentMapper.Map(dto);
        var contactDto = _createContactMapper.Map(dto);
        if (assignmentDto is null || contactDto is null)
        {
            return InvalidRequest("Resident contact payload is required.");
        }

        var result = await _bll.Residents.AddContactAsync(route.Value!, assignmentDto, contactDto, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : StatusCode(StatusCodes.Status201Created, _listMapper.Map(result.Value));
    }

    [HttpPut("{residentContactId:guid}")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> Update(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        ResidentContactAssignmentDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToResidentContactRoute(companySlug, residentIdCode, residentContactId);
        if (route.Result is not null)
        {
            return route.Result;
        }

        var bllDto = _assignmentMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Resident contact payload is required.");
        }

        var result = await _bll.Residents.UpdateContactAsync(route.Value!, bllDto, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_listMapper.Map(result.Value));
    }

    [HttpDelete("{residentContactId:guid}")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> Delete(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.RemoveContactAsync(route, cancellationToken),
            cancellationToken);
    }

    [HttpPost("{residentContactId:guid}/set-primary")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> SetPrimary(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.SetPrimaryContactAsync(route, cancellationToken),
            cancellationToken);
    }

    [HttpPost("{residentContactId:guid}/confirm")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> Confirm(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.ConfirmContactAsync(route, cancellationToken),
            cancellationToken);
    }

    [HttpPost("{residentContactId:guid}/unconfirm")]
    [ProducesResponseType(typeof(ResidentContactListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResidentContactListDto>> Unconfirm(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        CancellationToken cancellationToken)
    {
        return await RunContactCommandAsync(
            companySlug,
            residentIdCode,
            residentContactId,
            route => _bll.Residents.UnconfirmContactAsync(route, cancellationToken),
            cancellationToken);
    }

    private async Task<ActionResult<ResidentContactListDto>> RunContactCommandAsync(
        string companySlug,
        string residentIdCode,
        Guid residentContactId,
        Func<ResidentContactRoute, Task<Result>> command,
        CancellationToken cancellationToken)
    {
        var route = ToResidentContactRoute(companySlug, residentIdCode, residentContactId);
        if (route.Result is not null)
        {
            return route.Result;
        }

        var result = await command(route.Value!);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var listResult = await _bll.Residents.ListContactsAsync(route.Value!, cancellationToken);
        return listResult.IsFailed
            ? ToApiError(listResult.Errors)
            : Ok(_listMapper.Map(listResult.Value));
    }

    private ActionResult<ResidentRoute> ToResidentRoute(
        string companySlug,
        string residentIdCode)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new ResidentRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode
        };
    }

    private ActionResult<ResidentContactRoute> ToResidentContactRoute(
        string companySlug,
        string residentIdCode,
        Guid residentContactId)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        return new ResidentContactRoute
        {
            AppUserId = appUserId.Value,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            ResidentContactId = residentContactId
        };
    }
}
