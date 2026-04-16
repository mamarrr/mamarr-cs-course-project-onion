using System.Net;
using App.BLL.Management;
using App.DTO.v1;
using App.DTO.v1.Customer;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;

namespace WebApp.ApiControllers.Customer;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/profile")]
public class CustomerProfileController : ProfileApiControllerBase
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerProfileService _managementCustomerProfileService;

    public CustomerProfileController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerProfileService managementCustomerProfileService)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerProfileService = managementCustomerProfileService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<CustomerProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerProfileResponseDto>> GetProfile(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var profile = await _managementCustomerProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Customer profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new CustomerProfileResponseDto
        {
            Profile = MapProfile(profile, access.Context!)
        });
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CustomerProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerProfileResponseDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        [FromBody] UpdateCustomerProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementCustomerProfileService.UpdateProfileAsync(
            access.Context!,
            new CustomerProfileUpdateRequest
            {
                Name = dto.Name,
                RegistryCode = dto.RegistryCode,
                BillingEmail = dto.BillingEmail,
                BillingAddress = dto.BillingAddress,
                Phone = dto.Phone,
                IsActive = dto.IsActive
            },
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Customer profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.ErrorMessage ?? "Unable to update customer profile.",
                result.DuplicateRegistryCode ? ApiErrorCodes.Duplicate : ApiErrorCodes.BusinessRuleViolation,
                result.DuplicateRegistryCode
                    ? (nameof(dto.RegistryCode), result.ErrorMessage ?? "Unable to update customer profile.")
                    : (string.Empty, result.ErrorMessage ?? "Unable to update customer profile.")));
        }

        var profile = await _managementCustomerProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Customer profile was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new CustomerProfileResponseDto
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
        [FromBody] DeleteCustomerProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveDashboardAccessAsync(companySlug, customerSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var profile = await _managementCustomerProfileService.GetProfileAsync(access.Context!, cancellationToken);
        if (profile == null)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Customer profile was not found.", ApiErrorCodes.NotFound));
        }

        if (!string.Equals(dto.ConfirmationName?.Trim(), profile.Name.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                "Delete confirmation does not match the current customer name.",
                ApiErrorCodes.ValidationFailed,
                (nameof(dto.ConfirmationName), "Delete confirmation does not match the current customer name.")));
        }

        var result = await _managementCustomerProfileService.DeleteProfileAsync(access.Context!, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, "Customer profile was not found.", ApiErrorCodes.NotFound));
        }

        if (result.Forbidden)
        {
            return StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to delete customer profile.", ApiErrorCodes.BusinessRuleViolation));
        }

        return NoContent();
    }

    private async Task<(ManagementCustomerDashboardContext? Context, ActionResult? ErrorResult)> ResolveDashboardAccessAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var access = await _managementCustomerAccessService.ResolveDashboardAccessAsync(
            appUserId.Value,
            companySlug,
            customerSlug,
            cancellationToken);

        if (access.CompanyNotFound || access.CustomerNotFound)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Customer context was not found.", ApiErrorCodes.NotFound)));
        }

        if (access.IsForbidden || access.Context == null)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (access.Context, null);
    }

    private static CustomerProfileDto MapProfile(CustomerProfileModel profile, ManagementCustomerDashboardContext context)
    {
        return new CustomerProfileDto
        {
            CustomerId = profile.CustomerId,
            CustomerSlug = profile.CustomerSlug,
            Name = profile.Name,
            RegistryCode = profile.RegistryCode,
            BillingEmail = profile.BillingEmail,
            BillingAddress = profile.BillingAddress,
            Phone = profile.Phone,
            IsActive = profile.IsActive,
            RouteContext = new ApiRouteContextDto
            {
                CompanySlug = context.CompanySlug,
                CompanyName = context.CompanyName,
                CustomerSlug = profile.CustomerSlug,
                CustomerName = profile.Name,
                CurrentSection = "customer-profile"
            }
        };
    }
}
