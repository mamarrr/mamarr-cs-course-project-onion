using System.Net;
using App.BLL.CustomerWorkspace.Access;
using App.BLL.LeaseAssignments;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.UnitWorkspace.Access;
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
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}")]
public class UnitTenantsController : ProfileApiControllerBase
{
    private readonly ICustomerAccessService _customerAccessService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly IManagementLeaseService _managementLeaseService;
    private readonly IManagementLeaseSearchService _managementLeaseSearchService;

    public UnitTenantsController(
        ICustomerAccessService customerAccessService,
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        IManagementLeaseService managementLeaseService,
        IManagementLeaseSearchService managementLeaseSearchService)
    {
        _customerAccessService = customerAccessService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _managementLeaseService = managementLeaseService;
        _managementLeaseSearchService = managementLeaseSearchService;
    }

    [HttpGet("tenants")]
    [Produces("application/json")]
    [ProducesResponseType<UnitTenantsBootstrapResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitTenantsBootstrapResponseDto>> GetTenants(
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

        var leases = await _managementLeaseService.ListForUnitAsync(access.Context!, cancellationToken);
        var leaseRoles = await _managementLeaseSearchService.ListLeaseRolesAsync(cancellationToken);

        return Ok(new UnitTenantsBootstrapResponseDto
        {
            RouteContext = CreateRouteContext(access.Context!),
            Leases = leases.Leases.Select(MapLease).ToList(),
            LeaseRoles = leaseRoles.Roles.Select(MapLeaseRole).ToList()
        });
    }

    [HttpGet("resident-search")]
    [Produces("application/json")]
    [ProducesResponseType<UnitResidentSearchResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitResidentSearchResponseDto>> SearchResidents(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var result = await _managementLeaseSearchService.SearchResidentsAsync(access.Context!, searchTerm, cancellationToken);
        return Ok(new UnitResidentSearchResponseDto
        {
            Residents = result.Residents.Select(x => new UnitResidentSearchResultDto
            {
                ResidentId = x.ResidentId,
                FullName = x.FullName,
                IdCode = x.IdCode,
                IsActive = x.IsActive
            }).ToList()
        });
    }

    [HttpPost("leases")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<UnitLeaseCommandResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitLeaseCommandResponseDto>> CreateLease(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromBody] CreateUnitLeaseRequestDto dto,
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

        var result = await _managementLeaseService.CreateFromUnitAsync(
            access.Context!,
            new ManagementLeaseCreateRequest
            {
                ResidentId = dto.ResidentId!.Value,
                UnitId = access.Context!.UnitId,
                LeaseRoleId = dto.LeaseRoleId!.Value,
                StartDate = dto.StartDate!.Value,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                Notes = dto.Notes
            },
            cancellationToken);

        if (!result.Success || result.LeaseId == null)
        {
            return BadRequest(CreateLeaseCommandError(result, true));
        }

        return CreatedAtAction(
            nameof(GetTenants),
            new { version = "1.0", companySlug, customerSlug, propertySlug, unitSlug },
            new UnitLeaseCommandResponseDto { LeaseId = result.LeaseId.Value });
    }

    [HttpPut("leases/{leaseId:guid}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<UnitLeaseCommandResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitLeaseCommandResponseDto>> UpdateLease(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        [FromBody] UpdateUnitLeaseRequestDto dto,
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

        var result = await _managementLeaseService.UpdateFromUnitAsync(
            access.Context!,
            new ManagementLeaseUpdateRequest
            {
                LeaseId = leaseId,
                LeaseRoleId = dto.LeaseRoleId!.Value,
                StartDate = dto.StartDate!.Value,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                Notes = dto.Notes
            },
            cancellationToken);

        if (result.LeaseNotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, result.ErrorMessage ?? "Lease was not found.", ApiErrorCodes.NotFound));
        }

        if (!result.Success || result.LeaseId == null)
        {
            return BadRequest(CreateLeaseCommandError(result, false));
        }

        return Ok(new UnitLeaseCommandResponseDto { LeaseId = result.LeaseId.Value });
    }

    [HttpDelete("leases/{leaseId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteLease(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveUnitContextAsync(companySlug, customerSlug, propertySlug, unitSlug, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var result = await _managementLeaseService.DeleteFromUnitAsync(
            access.Context!,
            new ManagementLeaseDeleteRequest { LeaseId = leaseId },
            cancellationToken);

        if (result.LeaseNotFound)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, result.ErrorMessage ?? "Lease was not found.", ApiErrorCodes.NotFound));
        }

        if (!result.Success)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unable to delete lease.", ApiErrorCodes.BusinessRuleViolation));
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

    private RestApiErrorResponse CreateLeaseCommandError(ManagementLeaseCommandResult result, bool isCreate)
    {
        if (result.ResidentNotFound)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Resident was not found.", ApiErrorCodes.NotFound, (nameof(CreateUnitLeaseRequestDto.ResidentId), result.ErrorMessage ?? "Resident was not found."));
        }

        if (result.UnitNotFound)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unit was not found.", ApiErrorCodes.NotFound, (string.Empty, result.ErrorMessage ?? "Unit was not found."));
        }

        if (result.InvalidLeaseRole)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Lease role is invalid.", ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateUnitLeaseRequestDto.LeaseRoleId) : nameof(UpdateUnitLeaseRequestDto.LeaseRoleId)), result.ErrorMessage ?? "Lease role is invalid."));
        }

        if (result.InvalidStartDate)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Start date is invalid.", ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateUnitLeaseRequestDto.StartDate) : nameof(UpdateUnitLeaseRequestDto.StartDate)), result.ErrorMessage ?? "Start date is invalid."));
        }

        if (result.InvalidEndDate)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "End date is invalid.", ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateUnitLeaseRequestDto.EndDate) : nameof(UpdateUnitLeaseRequestDto.EndDate)), result.ErrorMessage ?? "End date is invalid."));
        }

        if (result.DuplicateActiveLease)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "An active lease for this resident already exists for the unit.", ApiErrorCodes.Conflict);
        }

        return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? (isCreate ? "Unable to create lease." : "Unable to update lease."), ApiErrorCodes.BusinessRuleViolation);
    }

    private static UnitTenantLeaseDto MapLease(ManagementUnitLeaseListItem item)
    {
        return new UnitTenantLeaseDto
        {
            LeaseId = item.LeaseId,
            ResidentId = item.ResidentId,
            UnitId = item.UnitId,
            PropertyId = item.PropertyId,
            ResidentFullName = item.ResidentFullName,
            ResidentIdCode = item.ResidentIdCode,
            LeaseRoleId = item.LeaseRoleId,
            LeaseRoleCode = item.LeaseRoleCode,
            LeaseRoleLabel = item.LeaseRoleLabel,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            IsActive = item.IsActive,
            Notes = item.Notes
        };
    }

    private static LookupOptionDto MapLeaseRole(ManagementLeaseRoleOption option)
    {
        return new LookupOptionDto
        {
            Id = option.LeaseRoleId,
            Code = option.Code,
            Label = option.Label
        };
    }

    private static ApiRouteContextDto CreateRouteContext(UnitDashboardContext context)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitSlug = context.UnitSlug,
            UnitName = context.UnitNr,
            CurrentSection = "unit-tenants"
        };
    }
}
