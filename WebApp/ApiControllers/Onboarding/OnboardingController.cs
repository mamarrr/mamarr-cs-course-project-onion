using System.Net;
using App.BLL.Onboarding;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.Api;
using App.DTO.v1;
using App.DTO.v1.Onboarding;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using App.Domain.Identity;
using WebApp.ApiControllers.Shared;

namespace WebApp.ApiControllers.Onboarding;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IApiOnboardingContextService _apiOnboardingContextService;
    private readonly IApiOnboardingRouteContextMapper _routeContextMapper;
    private readonly IOnboardingService _onboardingService;
    private readonly UserManager<AppUser> _userManager;

    public OnboardingController(
        IApiOnboardingContextService apiOnboardingContextService,
        IApiOnboardingRouteContextMapper routeContextMapper,
        IOnboardingService onboardingService,
        UserManager<AppUser> userManager)
    {
        _apiOnboardingContextService = apiOnboardingContextService;
        _routeContextMapper = routeContextMapper;
        _onboardingService = onboardingService;
        _userManager = userManager;
    }

    [HttpGet("contexts")]
    [Produces("application/json")]
    [ProducesResponseType<OnboardingContextsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    public async Task<ActionResult<OnboardingContextsResponseDto>> GetContexts(CancellationToken cancellationToken)
    {
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized));
        }

        var catalog = await _apiOnboardingContextService.GetContextsAsync(appUser.Id, cancellationToken);
        var response = _routeContextMapper.MapCatalog(catalog);

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
        var appUser = await _userManager.GetUserAsync(User);
        if (appUser == null)
        {
            return Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _onboardingService.CreateManagementCompanyAsync(new OnboardingCreateManagementCompanyRequest
        {
            AppUserId = appUser.Id,
            Name = dto.Name,
            RegistryCode = dto.RegistryCode,
            VatNumber = dto.VatNumber,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        });

        if (!result.Succeeded || result.ManagementCompanyId == null || string.IsNullOrWhiteSpace(result.ManagementCompanySlug))
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.Errors.FirstOrDefault() ?? "Unable to create management company.",
                result.Errors.Any(x => x.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                    ? ApiErrorCodes.Duplicate
                    : ApiErrorCodes.BusinessRuleViolation,
                result.Errors));
        }

        var response = new CreateManagementCompanyResponseDto
        {
            ManagementCompanyId = result.ManagementCompanyId.Value,
            ManagementCompanySlug = result.ManagementCompanySlug,
            RouteContext = _routeContextMapper.CreateManagementCompanyRouteContext(result.ManagementCompanySlug, dto.Name.Trim())
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
