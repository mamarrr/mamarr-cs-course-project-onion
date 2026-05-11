using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.WorkLogs;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.WorkLogs;
using App.DTO.v1.Portal.WorkLogs;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/tickets/{ticketId:guid}/scheduled-work/{scheduledWorkId:guid}/work-logs")]
public class WorkLogsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<WorkLogRequestDto, WorkLogBllDto> _requestMapper = new WorkLogApiMapper();
    private readonly WorkLogListItemApiMapper _responseMapper = new();

    public WorkLogsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [ProducesResponseType(typeof(WorkLogListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkLogListDto>> List(
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

        var result = await _bll.WorkLogs.ListForScheduledWorkAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_responseMapper.Map(result.Value));
    }

    [HttpGet("form")]
    [ProducesResponseType(typeof(WorkLogFormDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkLogFormDto>> CreateForm(
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

        var result = await _bll.WorkLogs.GetCreateFormAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_responseMapper.Map(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WorkLogDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkLogDto>> Add(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        WorkLogRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToScheduledWorkRoute(companySlug, ticketId, scheduledWorkId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _requestMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Work log payload is required.");
        }

        var result = await _bll.WorkLogs.AddAsync(route, bllDto, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var response = _responseMapper.Map(result.Value, companySlug, ticketId, scheduledWorkId);
        return Created(response.Path, response);
    }

    [HttpGet("{workLogId:guid}/form")]
    [ProducesResponseType(typeof(WorkLogFormDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkLogFormDto>> EditForm(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        CancellationToken cancellationToken)
    {
        var route = ToWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.WorkLogs.GetEditFormAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_responseMapper.Map(result.Value));
    }

    [HttpGet("{workLogId:guid}/delete-model")]
    [ProducesResponseType(typeof(WorkLogDeleteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkLogDeleteDto>> DeleteModel(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        CancellationToken cancellationToken)
    {
        var route = ToWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.WorkLogs.GetDeleteModelAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_responseMapper.Map(result.Value));
    }

    [HttpPut("{workLogId:guid}")]
    [ProducesResponseType(typeof(WorkLogDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkLogDto>> Update(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        WorkLogRequestDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _requestMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Work log payload is required.");
        }

        var result = await _bll.WorkLogs.UpdateAsync(route, bllDto, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.Map(result.Value, companySlug, ticketId, scheduledWorkId));
    }

    [HttpDelete("{workLogId:guid}")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Delete(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId,
        CancellationToken cancellationToken)
    {
        var route = ToWorkLogRoute(companySlug, ticketId, scheduledWorkId, workLogId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.WorkLogs.DeleteAsync(route, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Work log deleted."
        });
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

    private WorkLogRoute? ToWorkLogRoute(
        string companySlug,
        Guid ticketId,
        Guid scheduledWorkId,
        Guid workLogId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new WorkLogRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                TicketId = ticketId,
                ScheduledWorkId = scheduledWorkId,
                WorkLogId = workLogId
            };
    }
}
