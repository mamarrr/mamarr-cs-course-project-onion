using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Tickets;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.Tickets;
using App.DTO.v1.Portal.Tickets;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}/tickets")]
public class TicketsController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly TicketApiMapper _mapper = new();
    private readonly IBaseMapper<CreateTicketDto, TicketBllDto> _createMapper = new TicketApiMapper();
    private readonly IBaseMapper<UpdateTicketDto, TicketBllDto> _updateMapper = new TicketApiMapper();

    public TicketsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ManagementTicketsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ManagementTicketsDto>> Search(
        string companySlug,
        [FromQuery] TicketSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToManagementSearchRoute(companySlug, query);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.SearchAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}")]
    [ProducesResponseType(typeof(ContextTicketsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContextTicketsDto>> CustomerTickets(
        string companySlug,
        string customerSlug,
        [FromQuery] TicketSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToContextSearchRoute(companySlug, query, customerSlug: customerSlug);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.SearchCustomerTicketsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}")]
    [ProducesResponseType(typeof(ContextTicketsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContextTicketsDto>> PropertyTickets(
        string companySlug,
        string customerSlug,
        string propertySlug,
        [FromQuery] TicketSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToContextSearchRoute(
            companySlug,
            query,
            customerSlug: customerSlug,
            propertySlug: propertySlug);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.SearchPropertyTicketsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}")]
    [ProducesResponseType(typeof(ContextTicketsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContextTicketsDto>> UnitTickets(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromQuery] TicketSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToContextSearchRoute(
            companySlug,
            query,
            customerSlug: customerSlug,
            propertySlug: propertySlug,
            unitSlug: unitSlug);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.SearchUnitTicketsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("residents/{residentIdCode}")]
    [ProducesResponseType(typeof(ContextTicketsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContextTicketsDto>> ResidentTickets(
        string companySlug,
        string residentIdCode,
        [FromQuery] TicketSearchQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToContextSearchRoute(companySlug, query, residentIdCode: residentIdCode);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.SearchResidentTicketsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("form")]
    [ProducesResponseType(typeof(TicketFormDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketFormDto>> CreateForm(
        string companySlug,
        [FromQuery] TicketSelectorOptionsQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToSelectorOptionsRoute(companySlug, query);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.GetCreateFormAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("options")]
    [ProducesResponseType(typeof(TicketSelectorOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketSelectorOptionsDto>> SelectorOptions(
        string companySlug,
        [FromQuery] TicketSelectorOptionsQueryDto query,
        CancellationToken cancellationToken)
    {
        var route = ToSelectorOptionsRoute(companySlug, query);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.GetSelectorOptionsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketDto>> Create(
        string companySlug,
        CreateTicketDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToCompanyRoute(companySlug);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _createMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Ticket payload is required.");
        }

        var result = await _bll.Tickets.CreateAsync(route, bllDto, cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var response = _mapper.Map(result.Value)!;
        return Created($"/api/v1/portal/companies/{companySlug}/tickets/{response.TicketId:D}", response);
    }

    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDetailsDto>> Details(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.GetDetailsAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpGet("{ticketId:guid}/form")]
    [ProducesResponseType(typeof(TicketFormDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketFormDto>> EditForm(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.GetEditFormAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpPut("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> Update(
        string companySlug,
        Guid ticketId,
        UpdateTicketDto? dto,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Ticket payload is required.");
        }

        var result = await _bll.Tickets.UpdateAsync(route, bllDto, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpDelete("{ticketId:guid}")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> Delete(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.DeleteAsync(route, cancellationToken);
        return result.IsFailed
            ? ToApiError(result.Errors)
            : Ok(new CommandResultDto { Success = true, Message = "Ticket deleted." });
    }

    [HttpGet("{ticketId:guid}/transition-availability")]
    [ProducesResponseType(typeof(TicketTransitionAvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketTransitionAvailabilityDto>> TransitionAvailability(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.GetTransitionAvailabilityAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    [HttpPost("{ticketId:guid}/advance-status")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDto>> AdvanceStatus(
        string companySlug,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var route = ToTicketRoute(companySlug, ticketId);
        if (route is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Tickets.AdvanceStatusAsync(route, cancellationToken);
        return result.IsFailed ? ToApiError(result.Errors) : Ok(_mapper.Map(result.Value));
    }

    private ManagementCompanyRoute? ToCompanyRoute(string companySlug)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new ManagementCompanyRoute { AppUserId = appUserId.Value, CompanySlug = companySlug };
    }

    private TicketRoute? ToTicketRoute(string companySlug, Guid ticketId)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new TicketRoute { AppUserId = appUserId.Value, CompanySlug = companySlug, TicketId = ticketId };
    }

    private ManagementTicketSearchRoute? ToManagementSearchRoute(
        string companySlug,
        TicketSearchQueryDto query)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new ManagementTicketSearchRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                Search = query.Search,
                StatusId = query.StatusId,
                PriorityId = query.PriorityId,
                CategoryId = query.CategoryId,
                CustomerId = query.CustomerId,
                PropertyId = query.PropertyId,
                UnitId = query.UnitId,
                ResidentId = query.ResidentId,
                VendorId = query.VendorId,
                DueFrom = query.DueFrom,
                DueTo = query.DueTo
            };
    }

    private ContextTicketSearchRoute? ToContextSearchRoute(
        string companySlug,
        TicketSearchQueryDto query,
        string? customerSlug = null,
        string? propertySlug = null,
        string? unitSlug = null,
        string? residentIdCode = null)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new ContextTicketSearchRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                Search = query.Search,
                StatusId = query.StatusId,
                PriorityId = query.PriorityId,
                CategoryId = query.CategoryId,
                CustomerId = query.CustomerId,
                PropertyId = query.PropertyId,
                UnitId = query.UnitId,
                ResidentId = query.ResidentId,
                VendorId = query.VendorId,
                DueFrom = query.DueFrom,
                DueTo = query.DueTo,
                CustomerSlug = customerSlug,
                PropertySlug = propertySlug,
                UnitSlug = unitSlug,
                ResidentIdCode = residentIdCode
            };
    }

    private TicketSelectorOptionsRoute? ToSelectorOptionsRoute(
        string companySlug,
        TicketSelectorOptionsQueryDto query)
    {
        var appUserId = GetAppUserId();
        return appUserId is null
            ? null
            : new TicketSelectorOptionsRoute
            {
                AppUserId = appUserId.Value,
                CompanySlug = companySlug,
                CustomerId = query.CustomerId,
                PropertyId = query.PropertyId,
                UnitId = query.UnitId,
                CategoryId = query.CategoryId
            };
    }

}
