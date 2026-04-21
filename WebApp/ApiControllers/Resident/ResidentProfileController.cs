using System.Net;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Profiles;
using App.DTO.v1;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;

namespace WebApp.ApiControllers.Resident;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/re/{residentIdCode}/profile")]
public class ResidentProfileController : ProfileApiControllerBase
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly IResidentProfileService _residentProfileService;

    public ResidentProfileController(
        IResidentAccessService residentAccessService,
        IResidentProfileService residentProfileService)
    {
        _residentAccessService = residentAccessService;
        _residentProfileService = residentProfileService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ResidentProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentProfileResponseDto>> GetProfile(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var profile = await _residentProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Resident profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new ResidentProfileResponseDto
        {
            Profile = MapProfile(profile, access.Context!)
        });
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ResidentProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentProfileResponseDto>> UpdateProfile(
        string companySlug,
        string residentIdCode,
        [FromBody] UpdateResidentProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _residentProfileService.UpdateProfileAsync(
            access.Context!,
            new ResidentProfileUpdateRequest
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IdCode = dto.IdCode,
                PreferredLanguage = dto.PreferredLanguage,
                IsActive = dto.IsActive
            },
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Resident profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.ErrorMessage ?? "Unable to update resident profile.",
                result.DuplicateIdCode ? ApiErrorCodes.Duplicate : ApiErrorCodes.BusinessRuleViolation,
                result.DuplicateIdCode
                    ? (nameof(dto.IdCode), result.ErrorMessage ?? "Unable to update resident profile.")
                    : (string.Empty, result.ErrorMessage ?? "Unable to update resident profile.")));
        }

        var profile = await _residentProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Resident profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new ResidentProfileResponseDto
        {
            Profile = MapProfile(profile, access.Context!)
        });
    }

    [HttpDelete]
    [Consumes("application/json")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteProfile(
        string companySlug,
        string residentIdCode,
        [FromBody] DeleteResidentProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var profile = await _residentProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Resident profile was not found.", ApiErrorCodes.NotFound));
        }

        if (!string.Equals(dto.ConfirmationIdCode?.Trim(), profile.ResidentIdCode.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                "Delete confirmation does not match the current resident ID code.",
                ApiErrorCodes.ValidationFailed,
                (nameof(dto.ConfirmationIdCode), "Delete confirmation does not match the current resident ID code.")));
        }

        var result = await _residentProfileService.DeleteProfileAsync(access.Context!, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Resident profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to delete resident profile.", ApiErrorCodes.BusinessRuleViolation));
        }

        return NoContent();
    }

    private async Task<(ResidentDashboardContext? Context, ActionResult? ErrorResult)> ResolveDashboardAccessAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var access = await _residentAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            residentIdCode,
            cancellationToken);

        if (access.CompanyNotFound || access.ResidentNotFound)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Resident context was not found.", ApiErrorCodes.NotFound)));
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (access.Context, null);
    }

    private static ResidentProfileDto MapProfile(ResidentProfileModel profile, ResidentDashboardContext context)
    {
        return new ResidentProfileDto
        {
            ResidentId = profile.ResidentId,
            ResidentIdCode = profile.ResidentIdCode,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            PreferredLanguage = profile.PreferredLanguage,
            IsActive = profile.IsActive,
            RouteContext = new ApiRouteContextDto
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                ResidentIdCode = profile.ResidentIdCode,
                ResidentDisplayName = string.Join(' ', new[] { profile.FirstName, profile.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim(),
                CurrentSection = "resident-profile"
            }
        };
    }
}
