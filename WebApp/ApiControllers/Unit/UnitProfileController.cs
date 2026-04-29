using System.Net;
using App.BLL.Contracts.Properties.Services;
using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Workspace;
using App.DTO.v1;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Properties;

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}/profile")]
public class UnitProfileController : ProfileApiControllerBase
{
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IUnitProfileService _unitProfileService;
    private readonly PropertyApiMapper _propertyMapper;

    public UnitProfileController(
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IUnitProfileService unitProfileService,
        PropertyApiMapper propertyMapper)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _unitProfileService = unitProfileService;
        _propertyMapper = propertyMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<UnitProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitProfileResponseDto>> GetProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var profile = await _unitProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Unit profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new UnitProfileResponseDto
        {
            Profile = MapProfile(profile, access.Context!)
        });
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<UnitProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitProfileResponseDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromBody] UpdateUnitProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _unitProfileService.UpdateProfileAsync(
            access.Context!,
            new UnitProfileUpdateRequest
            {
                UnitNr = dto.UnitNr,
                FloorNr = dto.FloorNr,
                SizeM2 = dto.SizeM2,
                Notes = dto.Notes,
                IsActive = dto.IsActive
            },
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Unit profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to update unit profile.", ApiErrorCodes.BusinessRuleViolation));
        }

        var profile = await _unitProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Unit profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new UnitProfileResponseDto
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
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromBody] DeleteUnitProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var profile = await _unitProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Unit profile was not found.", ApiErrorCodes.NotFound));
        }

        if (!string.Equals(dto.ConfirmationUnitNr?.Trim(), profile.UnitNr.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                "Delete confirmation does not match the current unit number.",
                ApiErrorCodes.ValidationFailed,
                (nameof(dto.ConfirmationUnitNr), "Delete confirmation does not match the current unit number.")));
        }

        var result = await _unitProfileService.DeleteProfileAsync(access.Context!, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Unit profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to delete unit profile.", ApiErrorCodes.BusinessRuleViolation));
        }

        return NoContent();
    }

    private async Task<(UnitDashboardContext? Context, ActionResult? ErrorResult)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var propertyAccess = await _propertyWorkspaceService.GetWorkspaceAsync(
            _propertyMapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);
        if (propertyAccess.IsFailed)
        {
            return (null, propertyAccess.ToActionResult(_ => new UnitProfileResponseDto()).Result);
        }

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Value,
            unitSlug,
            cancellationToken);

        if (unitAccess.UnitNotFound || unitAccess.Context == null)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Unit context was not found.", ApiErrorCodes.NotFound)));
        }

        if (!unitAccess.IsAuthorized)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (unitAccess.Context, null);
    }

    private static UnitProfileDto MapProfile(UnitProfileModel profile, UnitDashboardContext context)
    {
        return new UnitProfileDto
        {
            UnitId = profile.UnitId,
            UnitSlug = profile.UnitSlug,
            UnitNr = profile.UnitNr,
            FloorNr = profile.FloorNr,
            SizeM2 = profile.SizeM2,
            Notes = profile.Notes,
            IsActive = profile.IsActive,
            RouteContext = new ApiRouteContextDto
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = context.PropertySlug,
                PropertyName = context.PropertyName,
                UnitSlug = profile.UnitSlug,
                UnitName = profile.UnitNr,
                CurrentSection = "unit-profile"
            }
        };
    }
}
