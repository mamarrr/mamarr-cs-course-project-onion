using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
using App.DAL.EF;
using App.DTO.v1;
using App.DTO.v1.Customer;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.ApiControllers.Customer;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr")]
public class CustomerPropertiesController : ControllerBase
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;
    private readonly AppDbContext _dbContext;

    public CustomerPropertiesController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService,
        AppDbContext dbContext)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
        _dbContext = dbContext;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<CustomerPropertiesResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerPropertiesResponseDto>> GetProperties(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolveCustomerContextAsync(companySlug, customerSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var context = access.Context!;
        var result = await _managementCustomerPropertyService.ListPropertiesAsync(context, cancellationToken);
        var propertyTypeOptions = await _dbContext.PropertyTypes
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new LookupOptionDto
            {
                Id = x.Id,
                Label = x.Label.ToString()
            })
            .ToListAsync(cancellationToken);

        return Ok(new CustomerPropertiesResponseDto
        {
            Properties = result.Properties.Select(x => new CustomerPropertySummaryDto
            {
                PropertyId = x.PropertyId,
                PropertySlug = x.PropertySlug,
                PropertyName = x.PropertyName,
                AddressLine = x.AddressLine,
                City = x.City,
                PostalCode = x.PostalCode,
                PropertyTypeId = x.PropertyTypeId,
                PropertyTypeCode = x.PropertyTypeCode,
                PropertyTypeLabel = x.PropertyTypeLabel,
                IsActive = x.IsActive,
                RouteContext = CreatePropertyRouteContext(context, x.PropertySlug, x.PropertyName)
            }).ToList(),
            PropertyTypeOptions = propertyTypeOptions
        });
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateCustomerPropertyResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreateCustomerPropertyResponseDto>> CreateProperty(
        string companySlug,
        string customerSlug,
        [FromBody] CreateCustomerPropertyRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveCustomerContextAsync(companySlug, customerSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementCustomerPropertyService.CreatePropertyAsync(
            access.Context!,
            new ManagementCustomerPropertyCreateRequest
            {
                Name = dto.Name,
                AddressLine = dto.AddressLine,
                City = dto.City,
                PostalCode = dto.PostalCode,
                PropertyTypeId = dto.PropertyTypeId!.Value,
                Notes = dto.Notes,
                IsActive = dto.IsActive
            },
            cancellationToken);

        if (!result.Success || result.CreatedPropertyId == null || string.IsNullOrWhiteSpace(result.CreatedPropertySlug))
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.ErrorMessage ?? "Unable to create property.",
                result.InvalidPropertyType ? ApiErrorCodes.ValidationFailed : ApiErrorCodes.BusinessRuleViolation,
                result.InvalidPropertyType ? (nameof(dto.PropertyTypeId), result.ErrorMessage ?? "Selected property type is invalid.") : (string.Empty, result.ErrorMessage ?? "Unable to create property.")));
        }

        var response = new CreateCustomerPropertyResponseDto
        {
            PropertyId = result.CreatedPropertyId.Value,
            PropertySlug = result.CreatedPropertySlug,
            RouteContext = CreatePropertyRouteContext(access.Context!, result.CreatedPropertySlug, dto.Name.Trim())
        };

        return CreatedAtAction(
            nameof(GetProperties),
            new { version = "1.0", companySlug, customerSlug },
            response);
    }

    private async Task<(ManagementCustomerDashboardContext? Context, ActionResult? ErrorResult)> ResolveCustomerContextAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var access = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (access.CompanyNotFound || access.CustomerNotFound)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Customer context was not found.", ApiErrorCodes.NotFound)));
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (access.Context, null);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private ApiRouteContextDto CreatePropertyRouteContext(
        ManagementCustomerDashboardContext context,
        string propertySlug,
        string propertyName)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = propertySlug,
            PropertyName = propertyName,
            CurrentSection = "property-dashboard"
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
