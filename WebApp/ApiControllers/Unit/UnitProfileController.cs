using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.PropertyWorkspace.Properties;
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

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}/profile")]
public class UnitProfileController : ProfileApiControllerBase
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IManagementUnitProfileService _managementUnitProfileService;

    public UnitProfileController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IManagementUnitProfileService managementUnitProfileService)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _managementUnitProfileService = managementUnitProfileService;
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

        var profile = await _managementUnitProfileService.GetProfileAsync(access.Context!, cancellationToken);
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

        var result = await _managementUnitProfileService.UpdateProfileAsync(
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

        var profile = await _managementUnitProfileService.GetProfileAsync(access.Context!, cancellationToken);
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

        var profile = await _managementUnitProfileService.GetProfileAsync(access.Context!, cancellationToken);
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

        var result = await _managementUnitProfileService.DeleteProfileAsync(access.Context!, cancellationToken);
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
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var customerAccess = await _customerAccessService.ResolveDashboardAccessAsync(
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

        var propertyAccess = await _propertyWorkspaceService.ResolvePropertyDashboardContextAsync(
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

        var unitAccess = await _unitAccessService.ResolveUnitDashboardContextAsync(
            propertyAccess.Context,
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
