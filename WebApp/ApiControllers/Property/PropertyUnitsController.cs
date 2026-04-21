using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.UnitWorkspace.Units;
using App.BLL.UnitWorkspace.Workspace;
using App.DTO.v1;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Property;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un")]
public class PropertyUnitsController : ControllerBase
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;
    private readonly IManagementPropertyUnitService _managementPropertyUnitService;

    public PropertyUnitsController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService,
        IManagementPropertyUnitService managementPropertyUnitService)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
        _managementPropertyUnitService = managementPropertyUnitService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PropertyUnitsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyUnitsResponseDto>> GetUnits(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var context = access.Context!;
        var result = await _managementPropertyUnitService.ListUnitsAsync(context, cancellationToken);

        return Ok(new PropertyUnitsResponseDto
        {
            Units = result.Units.Select(x => new PropertyUnitSummaryDto
            {
                UnitId = x.UnitId,
                UnitSlug = x.UnitSlug,
                UnitNr = x.UnitNr,
                FloorNr = x.FloorNr,
                SizeM2 = x.SizeM2,
                RouteContext = CreateUnitRouteContext(context, x.UnitSlug, x.UnitNr)
            }).ToList()
        });
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreatePropertyUnitResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreatePropertyUnitResponseDto>> CreateUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        [FromBody] CreatePropertyUnitRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementPropertyUnitService.CreateUnitAsync(
            access.Context!,
            new ManagementPropertyUnitCreateRequest
            {
                UnitNr = dto.UnitNr,
                FloorNr = dto.FloorNr,
                SizeM2 = dto.SizeM2,
                Notes = dto.Notes
            },
            cancellationToken);

        if (!result.Success || result.CreatedUnitId == null || string.IsNullOrWhiteSpace(result.CreatedUnitSlug))
        {
            var detail = result.InvalidUnitNr
                ? (nameof(dto.UnitNr), result.ErrorMessage ?? "Unit number is invalid.")
                : result.InvalidFloorNr
                    ? (nameof(dto.FloorNr), result.ErrorMessage ?? "Floor number is invalid.")
                    : result.InvalidSizeM2
                        ? (nameof(dto.SizeM2), result.ErrorMessage ?? "Unit size is invalid.")
                        : (string.Empty, result.ErrorMessage ?? "Unable to create unit.");

            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.ErrorMessage ?? "Unable to create unit.",
                ApiErrorCodes.ValidationFailed,
                detail));
        }

        var response = new CreatePropertyUnitResponseDto
        {
            UnitId = result.CreatedUnitId.Value,
            UnitSlug = result.CreatedUnitSlug,
            RouteContext = CreateUnitRouteContext(access.Context!, result.CreatedUnitSlug, dto.UnitNr.Trim())
        };

        return CreatedAtAction(
            nameof(GetUnits),
            new { version = "1.0", companySlug, customerSlug, propertySlug },
            response);
    }

    private async Task<(ManagementCustomerPropertyDashboardContext? Context, ActionResult? ErrorResult)> ResolvePropertyContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var customerAccess = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (customerAccess.CompanyNotFound || customerAccess.CustomerNotFound || customerAccess.Context == null)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Customer context was not found.", ApiErrorCodes.NotFound)));
        }

        if (customerAccess.IsForbidden)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        var propertyAccess = await _managementCustomerPropertyService.ResolvePropertyDashboardContextAsync(
            customerAccess.Context,
            propertySlug,
            cancellationToken);

        if (propertyAccess.PropertyNotFound || propertyAccess.Context == null)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Property context was not found.", ApiErrorCodes.NotFound)));
        }

        if (!propertyAccess.IsAuthorized)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (propertyAccess.Context, null);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private ApiRouteContextDto CreateUnitRouteContext(
        ManagementCustomerPropertyDashboardContext context,
        string unitSlug,
        string unitNr)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = unitSlug,
            UnitName = unitNr,
            CurrentSection = "property-units"
        };
    }

    private RestApiErrorResponse CreateValidationError()
    {
        return new RestApiErrorResponse
        {
            Status = HttpStatusCode.BadRequest,
            Error = "Validation failed.",
            ErrorCode = ApiErrorCodes.ValidationFailed,
            Errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray()),
            TraceId = HttpContext.TraceIdentifier
        };
    }

    private RestApiErrorResponse CreateError(HttpStatusCode status, string message, string code, params (string Key, string Message)[] details)
    {
        var errors = details
            .Where(x => !string.IsNullOrWhiteSpace(x.Message))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

        return new RestApiErrorResponse
        {
            Status = status,
            Error = message,
            ErrorCode = code,
            Errors = errors,
            TraceId = HttpContext.TraceIdentifier
        };
    }
}
