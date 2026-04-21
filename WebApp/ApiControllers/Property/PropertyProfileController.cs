using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.PropertyWorkspace.Profiles;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.Shared.Profiles;
using App.DTO.v1;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;

namespace WebApp.ApiControllers.Property;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/profile")]
public class PropertyProfileController : ProfileApiControllerBase
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerPropertyService _managementCustomerPropertyService;
    private readonly IManagementPropertyProfileService _managementPropertyProfileService;

    public PropertyProfileController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerPropertyService managementCustomerPropertyService,
        IManagementPropertyProfileService managementPropertyProfileService)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerPropertyService = managementCustomerPropertyService;
        _managementPropertyProfileService = managementPropertyProfileService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PropertyProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyProfileResponseDto>> GetProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var profile = await _managementPropertyProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Property profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new PropertyProfileResponseDto
        {
            Profile = MapProfile(profile, access.Context!)
        });
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<PropertyProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyProfileResponseDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        [FromBody] UpdatePropertyProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementPropertyProfileService.UpdateProfileAsync(
            access.Context!,
            new PropertyProfileUpdateRequest
            {
                Name = dto.Name,
                AddressLine = dto.AddressLine,
                City = dto.City,
                PostalCode = dto.PostalCode,
                Notes = dto.Notes,
                IsActive = dto.IsActive
            },
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Property profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to update property profile.", ApiErrorCodes.BusinessRuleViolation));
        }

        var profile = await _managementPropertyProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Property profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new PropertyProfileResponseDto
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
        [FromBody] DeletePropertyProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolvePropertyContextAsync(companySlug, customerSlug, propertySlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var profile = await _managementPropertyProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Property profile was not found.", ApiErrorCodes.NotFound));
        }

        if (!string.Equals(dto.ConfirmationName?.Trim(), profile.Name.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                "Delete confirmation does not match the current property name.",
                ApiErrorCodes.ValidationFailed,
                (nameof(dto.ConfirmationName), "Delete confirmation does not match the current property name.")));
        }

        var result = await _managementPropertyProfileService.DeleteProfileAsync(access.Context!, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Property profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to delete property profile.", ApiErrorCodes.BusinessRuleViolation));
        }

        return NoContent();
    }

    private async Task<(ManagementCustomerPropertyDashboardContext? Context, ActionResult? ErrorResult)> ResolvePropertyContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var customerAccess = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
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

        var propertyAccess = await _managementCustomerPropertyService.ResolvePropertyDashboardContextAsync(
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

        return (propertyAccess.Context, null);
    }

    private static PropertyProfileDto MapProfile(PropertyProfileModel profile, ManagementCustomerPropertyDashboardContext context)
    {
        return new PropertyProfileDto
        {
            PropertyId = profile.PropertyId,
            PropertySlug = profile.PropertySlug,
            Name = profile.Name,
            AddressLine = profile.AddressLine,
            City = profile.City,
            PostalCode = profile.PostalCode,
            Notes = profile.Notes,
            IsActive = profile.IsActive,
            RouteContext = new ApiRouteContextDto
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerSlug = context.CustomerSlug,
                CustomerName = context.CustomerName,
                PropertySlug = profile.PropertySlug,
                PropertyName = profile.Name,
                CurrentSection = "property-profile"
            }
        };
    }
}
