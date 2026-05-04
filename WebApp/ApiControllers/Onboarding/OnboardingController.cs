using System.Net;
using App.BLL.Contracts;
using App.BLL.Contracts.Onboarding.Commands;
using App.DTO.v1;
using App.DTO.v1.Onboarding;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Shared;
using WebApp.Services.Identity;

namespace WebApp.ApiControllers.Onboarding;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IApiOnboardingRouteContextMapper _routeContextMapper;
    private readonly IAppBLL _bll;
    private readonly IIdentityAccountService _identityAccountService;

    public OnboardingController(
        IApiOnboardingRouteContextMapper routeContextMapper,
        IAppBLL bll,
        IIdentityAccountService identityAccountService)
    {
        _routeContextMapper = routeContextMapper;
        _bll = bll;
        _identityAccountService = identityAccountService;
    }

    [HttpGet("contexts")]
    [Produces("application/json")]
    [ProducesResponseType<OnboardingContextsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    public async Task<ActionResult<OnboardingContextsResponseDto>> GetContexts(CancellationToken cancellationToken)
    {
        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, cancellationToken);
        if (appUserId == null)
        {
            return Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized));
        }

        var catalog = await _bll.ApiOnboardingContexts.GetContextsAsync(appUserId.Value, cancellationToken);
        var response = _routeContextMapper.MapCatalog(catalog.Value);

        return Ok(response);
    }

    [HttpPost("management-companies")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateManagementCompanyResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    public async Task<ActionResult<CreateManagementCompanyResponseDto>> CreateManagementCompany(
        [FromBody] CreateManagementCompanyRequestDto dto)
    {
        var appUserId = await _identityAccountService.GetAuthenticatedUserIdAsync(User, HttpContext.RequestAborted);
        if (appUserId == null)
        {
            return Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _bll.AccountOnboarding.CreateManagementCompanyAsync(new CreateManagementCompanyCommand
        {
            AppUserId = appUserId.Value,
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            VatNumber = dto.VatNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        }, HttpContext.RequestAborted);

        if (result.IsFailed)
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.Errors.FirstOrDefault()?.Message ?? "Unable to create management company.",
                result.Errors.Any(x => x.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    ? ApiErrorCodes.Duplicate
                    : ApiErrorCodes.BusinessRuleViolation,
                result.Errors.Select(error => error.Message)));
        }

        var response = new CreateManagementCompanyResponseDto
        {
            ManagementCompanyId = result.Value.ManagementCompanyId,
            ManagementCompanySlug = result.Value.ManagementCompanySlug,
            RouteContext = _routeContextMapper.CreateManagementCompanyRouteContext(result.Value.ManagementCompanySlug, dto.Name.Trim())
        };

        return CreatedAtAction(nameof(GetContexts), new { version = "1.0" }, response);
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

    private RestApiErrorResponse CreateError(HttpStatusCode status, string message, string code, params IEnumerable<string>[] details)
    {
        var errorDetails = details.SelectMany(x => x).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return new RestApiErrorResponse
        {
            Status = status,
            Error = message,
            ErrorCode = code,
            Errors = errorDetails.Length == 0
                ? new Dictionary<string, string[]>()
                : new Dictionary<string, string[]> { [string.Empty] = errorDetails },
            TraceId = HttpContext.TraceIdentifier
        };
    }
}
