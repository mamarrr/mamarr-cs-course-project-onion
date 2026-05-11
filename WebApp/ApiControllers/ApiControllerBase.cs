using System.Net;
using System.Security.Claims;
using App.BLL.DTO.Common.Errors;
using App.DTO.v1;
using App.DTO.v1.Shared;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid? GetAppUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userId, out var appUserId)
            ? appUserId
            : null;
    }

    protected ActionResult ToApiError(IReadOnlyList<IError> errors)
    {
        var firstError = errors.FirstOrDefault();
        var statusCode = ResolveStatusCode(firstError);
        var response = new RestApiErrorResponse
        {
            Status = (HttpStatusCode)statusCode,
            Error = firstError?.Message ?? "Request failed.",
            ErrorCode = ResolveErrorCode(firstError),
            TraceId = HttpContext.TraceIdentifier
        };

        if (firstError is ValidationAppError validationError)
        {
            foreach (var group in validationError.Failures.GroupBy(failure => failure.PropertyName))
            {
                response.Errors[group.Key] = group.Select(failure => failure.ErrorMessage).ToArray();
            }
        }

        return StatusCode(statusCode, response);
    }

    protected ActionResult InvalidRequest(string message)
    {
        return BadRequest(new RestApiErrorResponse
        {
            Status = HttpStatusCode.BadRequest,
            Error = message,
            ErrorCode = ApiErrorCodes.ValidationFailed,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    protected ActionResult UnauthorizedRequest(string message)
    {
        return Unauthorized(new RestApiErrorResponse
        {
            Status = HttpStatusCode.Unauthorized,
            Error = message,
            ErrorCode = ApiErrorCodes.Unauthorized,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    protected ActionResult ForbiddenRequest(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new RestApiErrorResponse
        {
            Status = HttpStatusCode.Forbidden,
            Error = message,
            ErrorCode = ApiErrorCodes.Forbidden,
            TraceId = HttpContext.TraceIdentifier
        });
    }

    private static int ResolveStatusCode(IError? error)
    {
        return error switch
        {
            UnauthorizedError => StatusCodes.Status401Unauthorized,
            ForbiddenError => StatusCodes.Status403Forbidden,
            NotFoundError => StatusCodes.Status404NotFound,
            ConflictError => StatusCodes.Status409Conflict,
            ValidationAppError => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string ResolveErrorCode(IError? error)
    {
        return error switch
        {
            UnauthorizedError => ApiErrorCodes.Unauthorized,
            ForbiddenError => ApiErrorCodes.Forbidden,
            NotFoundError => ApiErrorCodes.NotFound,
            ConflictError => ApiErrorCodes.Conflict,
            ValidationAppError => ApiErrorCodes.ValidationFailed,
            _ => ApiErrorCodes.BusinessRuleViolation
        };
    }
}
