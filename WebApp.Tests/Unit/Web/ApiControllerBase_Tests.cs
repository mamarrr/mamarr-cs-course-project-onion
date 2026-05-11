using System.Net;
using System.Security.Claims;
using App.BLL.DTO.Common;
using App.BLL.DTO.Common.Errors;
using App.DTO.v1;
using App.DTO.v1.Shared;
using AwesomeAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers;

namespace WebApp.Tests.Unit.Web;

public class ApiControllerBase_Tests
{
    private static readonly Guid UserId = new("10000000-0000-0000-0000-000000000001");

    [Fact]
    public void GetAppUserId_ReadsNameIdentifierClaim()
    {
        var controller = CreateController(new Claim(ClaimTypes.NameIdentifier, UserId.ToString()));

        controller.ExposedGetAppUserId().Should().Be(UserId);
    }

    [Fact]
    public void GetAppUserId_ReadsSubClaim_WhenNameIdentifierIsAbsent()
    {
        var controller = CreateController(new Claim("sub", UserId.ToString()));

        controller.ExposedGetAppUserId().Should().Be(UserId);
    }

    [Fact]
    public void GetAppUserId_InvalidClaim_ReturnsNull()
    {
        var controller = CreateController(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        controller.ExposedGetAppUserId().Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(ErrorCases))]
    public void ToApiError_MapsBusinessErrors(
        IError error,
        int expectedStatusCode,
        HttpStatusCode expectedStatus,
        string expectedErrorCode)
    {
        var controller = CreateController();

        var result = controller.ExposedToApiError([error]);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatusCode);
        var body = objectResult.Value.Should().BeOfType<RestApiErrorResponse>().Subject;
        body.Status.Should().Be(expectedStatus);
        body.Error.Should().Be(error.Message);
        body.ErrorCode.Should().Be(expectedErrorCode);
        body.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public void ToApiError_MapsValidationErrorsWithFieldFailures()
    {
        var controller = CreateController();
        var error = new ValidationAppError(
            "Validation failed.",
            [
                new ValidationFailureModel { PropertyName = "Email", ErrorMessage = "Email is required." },
                new ValidationFailureModel { PropertyName = "Email", ErrorMessage = "Email is invalid." },
                new ValidationFailureModel { PropertyName = "Password", ErrorMessage = "Password is required." }
            ]);

        var result = controller.ExposedToApiError([error]);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = objectResult.Value.Should().BeOfType<RestApiErrorResponse>().Subject;
        body.Status.Should().Be(HttpStatusCode.BadRequest);
        body.ErrorCode.Should().Be(ApiErrorCodes.ValidationFailed);
        body.Errors["Email"].Should().BeEquivalentTo("Email is required.", "Email is invalid.");
        body.Errors["Password"].Should().BeEquivalentTo("Password is required.");
    }

    [Fact]
    public void ToApiError_UnknownError_MapsToBusinessRuleViolation()
    {
        var controller = CreateController();
        var error = new Error("Unknown business failure.");

        var result = controller.ExposedToApiError([error]);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = objectResult.Value.Should().BeOfType<RestApiErrorResponse>().Subject;
        body.Status.Should().Be(HttpStatusCode.BadRequest);
        body.Error.Should().Be("Unknown business failure.");
        body.ErrorCode.Should().Be(ApiErrorCodes.BusinessRuleViolation);
        body.TraceId.Should().Be("trace-1");
    }

    [Fact]
    public void ToApiError_EmptyErrorList_MapsToGenericBadRequest()
    {
        var controller = CreateController();

        var result = controller.ExposedToApiError([]);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = objectResult.Value.Should().BeOfType<RestApiErrorResponse>().Subject;
        body.Status.Should().Be(HttpStatusCode.BadRequest);
        body.Error.Should().Be("Request failed.");
        body.ErrorCode.Should().Be(ApiErrorCodes.BusinessRuleViolation);
        body.TraceId.Should().Be("trace-1");
    }

    public static TheoryData<IError, int, HttpStatusCode, string> ErrorCases => new()
    {
        { new UnauthorizedError("Unauthorized."), StatusCodes.Status401Unauthorized, HttpStatusCode.Unauthorized, ApiErrorCodes.Unauthorized },
        { new ForbiddenError("Forbidden."), StatusCodes.Status403Forbidden, HttpStatusCode.Forbidden, ApiErrorCodes.Forbidden },
        { new NotFoundError("Missing."), StatusCodes.Status404NotFound, HttpStatusCode.NotFound, ApiErrorCodes.NotFound },
        { new ConflictError("Conflict."), StatusCodes.Status409Conflict, HttpStatusCode.Conflict, ApiErrorCodes.Conflict }
    };

    private static TestApiController CreateController(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-1",
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        return new TestApiController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private sealed class TestApiController : ApiControllerBase
    {
        public Guid? ExposedGetAppUserId()
        {
            return GetAppUserId();
        }

        public ActionResult ExposedToApiError(IReadOnlyList<IError> errors)
        {
            return ToApiError(errors);
        }
    }
}
