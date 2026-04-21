using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.DTO.v1;
using App.DTO.v1.Customer;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Customer;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/dashboard")]
public class CustomerDashboardController : ControllerBase
{
    private readonly ICustomerAccessService _customerAccessService;

    public CustomerDashboardController(ICustomerAccessService customerAccessService)
    {
        _customerAccessService = customerAccessService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<CustomerDashboardResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerDashboardResponseDto>> GetDashboard(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var context = access.Context!;
        return Ok(new CustomerDashboardResponseDto
        {
            Dashboard = new ApiDashboardDto
            {
                RouteContext = new ApiRouteContextDto
                {
                    CompanySlug = context.CompanySlug,
                    CompanyName = context.CompanyName,
                    CustomerSlug = context.CustomerSlug,
                    CustomerName = context.CustomerName,
                    CurrentSection = "customer-dashboard"
                },
                Title = "Customer dashboard",
                SectionLabel = "Dashboard",
                Widgets = Array.Empty<string>()
            }
        });
    }

    private async Task<(CustomerWorkspaceDashboardContext? Context, ActionResult? ErrorResult)> ResolveDashboardAccessAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var access = await _customerAccessService.ResolveDashboardAccessAsync(
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
