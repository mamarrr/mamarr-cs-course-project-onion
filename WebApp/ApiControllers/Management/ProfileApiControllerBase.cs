using System.Net;
using App.DTO.v1;
using App.DTO.v1.Shared;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Management;

public abstract class ProfileApiControllerBase : ControllerBase
{
    protected Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    protected RestApiErrorResponse CreateValidationError()
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

    protected RestApiErrorResponse CreateError(HttpStatusCode status, string message, string code, params (string Key, string Message)[] details)
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

    protected ApiDashboardDto CreateDashboard(string title, string sectionLabel, ApiRouteContextDto routeContext)
    {
        return new ApiDashboardDto
        {
            RouteContext = routeContext,
            Title = title,
            SectionLabel = sectionLabel,
            Widgets = Array.Empty<string>()
        };
    }
}
