using System.Net;
using App.BLL.LeaseAssignments;
using App.BLL.Contracts.Properties.Services;
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
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Properties;

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}")]
public class UnitTenantsController : ProfileApiControllerBase
{
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly IUnitAccessService _unitAccessService;
    private readonly ILeaseAssignmentService _leaseAssignmentService;
    private readonly ILeaseLookupService _leaseLookupService;
    private readonly PropertyApiMapper _propertyMapper;

    public UnitTenantsController(
        IPropertyWorkspaceService propertyWorkspaceService,
        IUnitAccessService unitAccessService,
        ILeaseAssignmentService leaseAssignmentService,
        ILeaseLookupService leaseLookupService,
        PropertyApiMapper propertyMapper)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _unitAccessService = unitAccessService;
        _leaseAssignmentService = leaseAssignmentService;
        _leaseLookupService = leaseLookupService;
        _propertyMapper = propertyMapper;
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

        var leases = await _leaseAssignmentService.ListForUnitAsync(access.Context!, cancellationToken);
        var leaseRoles = await _leaseLookupService.ListLeaseRolesAsync(cancellationToken);

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

        var result = await _leaseLookupService.SearchResidentsAsync(access.Context!, searchTerm, cancellationToken);
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

        var result = await _leaseAssignmentService.CreateFromUnitAsync(
            access.Context!,
            new LeaseCreateRequest
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

        var result = await _leaseAssignmentService.UpdateFromUnitAsync(
            access.Context!,
            new LeaseUpdateRequest
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

        var result = await _leaseAssignmentService.DeleteFromUnitAsync(
            access.Context!,
            new LeaseDeleteRequest { LeaseId = leaseId },
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
        var propertyAccess = await _propertyWorkspaceService.GetWorkspaceAsync(
            _propertyMapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);
        if (propertyAccess.IsFailed)
        {
            return (null, propertyAccess.ToActionResult(_ => new UnitTenantsBootstrapResponseDto()).Result);
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

    private RestApiErrorResponse CreateLeaseCommandError(LeaseCommandResult result, bool isCreate)
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

    private static UnitTenantLeaseDto MapLease(UnitLeaseListItem item)
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

    private static LookupOptionDto MapLeaseRole(LeaseRoleOption option)
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
