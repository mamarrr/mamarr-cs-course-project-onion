using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ScheduledWorks;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.ScheduledWork;
using App.DTO.v1.Portal.ScheduledWork;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work")]
public class ScheduledWorkController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CreateScheduledWorkDto, ScheduledWorkBllDto> _createMapper = new ScheduledWorkApiMapper();
    private readonly IBaseMapper<UpdateScheduledWorkDto, ScheduledWorkBllDto> _updateMapper = new ScheduledWorkApiMapper();
    private readonly ScheduledWorkApiMapper _commandResponseMapper = new();
    private readonly ScheduledWorkDetailsApiMapper _detailsMapper = new();
    private readonly ScheduledWorkListItemApiMapper _listMapper = new();

    public ScheduledWorkController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ScheduledWorkListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledWorkListDto>> List(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.ScheduledWorks.ListForTicketAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_listMapper.Map(result.Value));
    }

    [HttpGet("form")]
    [ProducesResponseType(typeof(ScheduledWorkFormDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledWorkFormDto>> CreateForm(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.ScheduledWorks.GetCreateFormAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_detailsMapper.Map(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScheduledWorkDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ScheduledWorkDto>> Schedule(
        string companySlug,
        Guid ticketId,
        CreateScheduledWorkDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _createMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Scheduled work payload is required.");
        }

        var result = await _bll.ScheduledWorks.ScheduleAsync(route, bllDto, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var response = _commandResponseMapper.Map(result.Value, companySlug, ticketId);
        return Created(response.Path, response);
    }

    [HttpGet("{scheduledWorkId:guid}")]
    [ProducesResponseType(typeof(ScheduledWorkDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledWorkDetailsDto>> Details(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var route = ToScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.ScheduledWorks.GetDetailsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_detailsMapper.Map(result.Value));
    }

    [HttpGet("{scheduledWorkId:guid}/form")]
    [ProducesResponseType(typeof(ScheduledWorkFormDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledWorkFormDto>> EditForm(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        var route = ToScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.ScheduledWorks.GetEditFormAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_detailsMapper.Map(result.Value));
    }

    [HttpPut("{scheduledWorkId:guid}")]
    [ProducesResponseType(typeof(ScheduledWorkDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledWorkDto>> Update(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        UpdateScheduledWorkDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Scheduled work payload is required.");
        }

        var result = await _bll.ScheduledWorks.UpdateScheduleAsync(route, bllDto, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(_commandResponseMapper.Map(result.Value, companySlug, ticketId));
    }

    [HttpPost("{scheduledWorkId:guid}/start")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Start(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        ScheduledWorkActionDto dto,
        CancellationToken cancellationToken)
    {
        return await RunCommandAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.StartWorkAsync(route, dto.ActionAt, cancellationToken),
            "Scheduled work started.");
    }

    [HttpPost("{scheduledWorkId:guid}/complete")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Complete(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        ScheduledWorkActionDto dto,
        CancellationToken cancellationToken)
    {
        return await RunCommandAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.CompleteWorkAsync(route, dto.ActionAt, cancellationToken),
            "Scheduled work completed.");
    }

    [HttpPost("{scheduledWorkId:guid}/cancel")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Cancel(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        return await RunCommandAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.CancelWorkAsync(route, cancellationToken),
            "Scheduled work cancelled.");
    }

    [HttpDelete("{scheduledWorkId:guid}")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Delete(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        CancellationToken cancellationToken)
    {
        return await RunCommandAsync(
            companySlug,
            ticketId,
            scheduledWorkId,
            route => _bll.ScheduledWorks.DeleteAsync(route, cancellationToken),
            "Scheduled work deleted.");
    }

    private async Task<ActionResult<CommandResultDto>> RunCommandAsync(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Func<ScheduledWorkRoute, Task<FluentResults.Result>> command,
        string message)
    {
        var route = ToScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await command(route);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(new CommandResultDto { Success = true, Message = message });
    }

    private TicketRoute? ToTicketRoute(string companySlug, Guid ticketId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new TicketRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                TicketId = ticketId
            };
    }

    private ScheduledWorkRoute? ToScheduledWorkRoute(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new ScheduledWorkRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                TicketId = ticketId,
                ScheduledWorkId = scheduledWorkId
            };
    }
}
