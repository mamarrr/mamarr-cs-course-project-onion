using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Properties;
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
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/dashboard")]
public class PropertyDashboardController : ControllerBase
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;

    public PropertyDashboardController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PropertyDashboardResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyDashboardResponseDto>> GetDashboard(
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
        return Ok(new PropertyDashboardResponseDto
        {
            Dashboard = new ApiDashboardDto
            {
                RouteContext = new ApiRouteContextDto
                {
                    CompanySlug = context.CompanySlug,
                    CompanyName = context.CompanyName,
                    CustomerSlug = context.CustomerSlug,
                    CustomerName = context.CustomerName,
                    PropertySlug = context.PropertySlug,
                    PropertyName = context.PropertyName,
                    CurrentSection = "property-dashboard"
                },
                Title = "Property dashboard",
                SectionLabel = "Dashboard",
                Widgets = Array.Empty<string>()
            }
        });
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

    private RestApiErrorResponse CreateError(HttpStatusCode status, string message, string code)
    {
        return new RestApiErrorResponse
        {
            Status = status,
            Error = message,
            ErrorCode = code,
            Errors = new Dictionary<string, string[]>(),
            TraceId = HttpContext.TraceIdentifier
        };
    }
}
