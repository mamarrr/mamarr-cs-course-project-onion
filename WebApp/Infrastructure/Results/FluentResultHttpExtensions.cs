using System.Diagnostics;
using System.Net;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Customers.Errors;
using App.BLL.Contracts.Residents.Errors;
using App.DTO.v1;
using App.DTO.v1.Shared;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Infrastructure.Results;

public static class FluentResultHttpExtensions
{
    public static ActionResult<TResponse> ToActionResult<TModel, TResponse>(
        this Result<TModel> result,
        Func<TModel, TResponse> map)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(map(result.Value));
        }

        return ToFailureActionResult(result.Errors);
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return ToFailureActionResult(result.Errors);
    }

    private static ObjectResult ToFailureActionResult(IReadOnlyList<IError> errors)
    {
        var validationError = errors.OfType<ValidationAppError>().FirstOrDefault();
        if (validationError is not null)
        {
            return CreateObjectResult(
                HttpStatusCode.BadRequest,
                validationError.Message,
                ApiErrorCodes.ValidationFailed,
                BuildValidationErrors(validationError));
        }

        var duplicateRegistryCodeError = errors.OfType<DuplicateRegistryCodeError>().FirstOrDefault();
        if (duplicateRegistryCodeError is not null)
        {
            return CreateObjectResult(
                HttpStatusCode.BadRequest,
                duplicateRegistryCodeError.Message,
                ApiErrorCodes.Duplicate,
                new Dictionary<string, string[]>
                {
                    [duplicateRegistryCodeError.PropertyName] = [duplicateRegistryCodeError.Message]
                });
        }

        var duplicateResidentIdCodeError = errors.OfType<DuplicateResidentIdCodeError>().FirstOrDefault();
        if (duplicateResidentIdCodeError is not null)
        {
            return CreateObjectResult(
                HttpStatusCode.BadRequest,
                duplicateResidentIdCodeError.Message,
                ApiErrorCodes.Duplicate,
                new Dictionary<string, string[]>
                {
                    [duplicateResidentIdCodeError.PropertyName] = [duplicateResidentIdCodeError.Message]
                });
        }

        var residentValidationError = errors.OfType<ResidentValidationError>().FirstOrDefault();
        if (residentValidationError is not null)
        {
            return CreateObjectResult(
                HttpStatusCode.BadRequest,
                residentValidationError.Message,
                ApiErrorCodes.BusinessRuleViolation,
                BuildResidentValidationErrors(residentValidationError));
        }

        var error = errors.FirstOrDefault();

        return error switch
        {
            NotFoundError => CreateObjectResult(HttpStatusCode.NotFound, error.Message, ApiErrorCodes.NotFound),
            ForbiddenError => CreateObjectResult(HttpStatusCode.Forbidden, error.Message, ApiErrorCodes.Forbidden),
            UnauthorizedError => CreateObjectResult(HttpStatusCode.Unauthorized, error.Message, ApiErrorCodes.Unauthorized),
            ConflictError => CreateObjectResult(HttpStatusCode.Conflict, error.Message, ApiErrorCodes.Conflict),
            BusinessRuleError => CreateObjectResult(HttpStatusCode.BadRequest, error.Message, ApiErrorCodes.BusinessRuleViolation),
            UnexpectedAppError => CreateObjectResult(HttpStatusCode.InternalServerError, error.Message, "unexpected_error"),
            _ => CreateObjectResult(
                HttpStatusCode.InternalServerError,
                error?.Message ?? "An unexpected error occurred.",
                "unexpected_error")
        };
    }

    private static ObjectResult CreateObjectResult(
        HttpStatusCode status,
        string message,
        string errorCode,
        Dictionary<string, string[]>? errors = null)
    {
        return new ObjectResult(new RestApiErrorResponse
        {
            Status = status,
            Error = message,
            ErrorCode = errorCode,
            Errors = errors ?? new Dictionary<string, string[]>(),
            TraceId = Activity.Current?.Id
        })
        {
            StatusCode = (int)status
        };
    }

    private static Dictionary<string, string[]> BuildValidationErrors(ValidationAppError error)
    {
        return error.Failures
            .GroupBy(failure => failure.PropertyName ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());
    }

    private static Dictionary<string, string[]> BuildResidentValidationErrors(ResidentValidationError error)
    {
        return error.Failures
            .GroupBy(failure => failure.PropertyName ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());
    }
}
