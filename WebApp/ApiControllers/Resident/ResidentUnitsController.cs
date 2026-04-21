using System.Net;
using App.BLL.LeaseAssignments;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Residents;
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
[Route("/api/v{version:apiVersion}/co/{companySlug}/re/{residentIdCode}")]
public class ResidentUnitsController : ProfileApiControllerBase
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly IManagementLeaseService _managementLeaseService;
    private readonly IManagementLeaseSearchService _managementLeaseSearchService;

    public ResidentUnitsController(
        IResidentAccessService residentAccessService,
        IManagementLeaseService managementLeaseService,
        IManagementLeaseSearchService managementLeaseSearchService)
    {
        _residentAccessService = residentAccessService;
        _managementLeaseService = managementLeaseService;
        _managementLeaseSearchService = managementLeaseSearchService;
    }

    [HttpGet("units")]
    [Produces("application/json")]
    [ProducesResponseType<ResidentUnitsBootstrapResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentUnitsBootstrapResponseDto>> GetUnits(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var leases = await _managementLeaseService.ListForResidentAsync(access.Context!, cancellationToken);
        var leaseRoles = await _managementLeaseSearchService.ListLeaseRolesAsync(cancellationToken);

        return Ok(new ResidentUnitsBootstrapResponseDto
        {
            RouteContext = CreateRouteContext(access.Context!),
            Leases = leases.Leases.Select(MapLease).ToList(),
            LeaseRoles = leaseRoles.Roles.Select(MapLeaseRole).ToList()
        });
    }

    [HttpGet("property-search")]
    [Produces("application/json")]
    [ProducesResponseType<ResidentPropertySearchResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentPropertySearchResponseDto>> SearchProperties(
        string companySlug,
        string residentIdCode,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var result = await _managementLeaseSearchService.SearchPropertiesAsync(access.Context!, searchTerm, cancellationToken);
        return Ok(new ResidentPropertySearchResponseDto
        {
            Properties = result.Properties.Select(x => new ResidentPropertySearchResultDto
            {
                PropertyId = x.PropertyId,
                CustomerId = x.CustomerId,
                PropertySlug = x.PropertySlug,
                PropertyName = x.PropertyName,
                CustomerSlug = x.CustomerSlug,
                CustomerName = x.CustomerName,
                AddressLine = x.AddressLine,
                City = x.City,
                PostalCode = x.PostalCode
            }).ToList()
        });
    }

    [HttpGet("properties/{propertyId:guid}/units")]
    [Produces("application/json")]
    [ProducesResponseType<ResidentPropertyUnitsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentPropertyUnitsResponseDto>> ListUnitsForProperty(
        string companySlug,
        string residentIdCode,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var result = await _managementLeaseSearchService.ListUnitsForPropertyAsync(access.Context!, propertyId, cancellationToken);
        if (!result.Success)
        {
            return NotFound(CreateError(HttpStatusCode.NotFound, result.ErrorMessage ?? "Property was not found.", ApiErrorCodes.NotFound));
        }

        return Ok(new ResidentPropertyUnitsResponseDto
        {
            Units = result.Units.Select(x => new ResidentPropertyUnitOptionDto
            {
                UnitId = x.UnitId,
                UnitSlug = x.UnitSlug,
                UnitNr = x.UnitNr,
                FloorNr = x.FloorNr,
                IsActive = x.IsActive
            }).ToList()
        });
    }

    [HttpPost("leases")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<ResidentLeaseCommandResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentLeaseCommandResponseDto>> CreateLease(
        string companySlug,
        string residentIdCode,
        [FromBody] CreateResidentLeaseRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementLeaseService.CreateFromResidentAsync(
            access.Context!,
            new ManagementLeaseCreateRequest
            {
                ResidentId = access.Context!.ResidentId,
                UnitId = dto.UnitId!.Value,
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
            nameof(GetUnits),
            new { version = "1.0", companySlug, residentIdCode },
            new ResidentLeaseCommandResponseDto { LeaseId = result.LeaseId.Value });
    }

    [HttpPut("leases/{leaseId:guid}")]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<ResidentLeaseCommandResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentLeaseCommandResponseDto>> UpdateLease(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        [FromBody] UpdateResidentLeaseRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementLeaseService.UpdateFromResidentAsync(
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

        return Ok(new ResidentLeaseCommandResponseDto { LeaseId = result.LeaseId.Value });
    }

    [HttpDelete("leases/{leaseId:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteLease(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var access = await ResolveResidentContextAsync(companySlug, residentIdCode, cancellationToken);
        if (access.ErrorResult != null)
        {
            return access.ErrorResult;
        }

        var result = await _managementLeaseService.DeleteFromResidentAsync(
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

    private async Task<(ResidentDashboardContext? Context, ActionResult? ErrorResult)> ResolveResidentContextAsync(
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

        if (access.CompanyNotFound || access.ResidentNotFound || access.Context == null)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Resident context was not found.", ApiErrorCodes.NotFound)));
        }

        if (access.IsForbidden)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (access.Context, null);
    }

    private RestApiErrorResponse CreateLeaseCommandError(ManagementLeaseCommandResult result, bool isCreate)
    {
        if (result.PropertyNotFound)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Property was not found.", ApiErrorCodes.NotFound, (string.Empty, result.ErrorMessage ?? "Property was not found."));
        }

        if (result.UnitNotFound)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Unit was not found.", ApiErrorCodes.NotFound, ((isCreate ? nameof(CreateResidentLeaseRequestDto.UnitId) : string.Empty), result.ErrorMessage ?? "Unit was not found."));
        }

        if (result.InvalidLeaseRole)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Lease role is invalid.", ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateResidentLeaseRequestDto.LeaseRoleId) : nameof(UpdateResidentLeaseRequestDto.LeaseRoleId)), result.ErrorMessage ?? "Lease role is invalid."));
        }

        if (result.InvalidStartDate)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "Start date is invalid.", ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateResidentLeaseRequestDto.StartDate) : nameof(UpdateResidentLeaseRequestDto.StartDate)), result.ErrorMessage ?? "Start date is invalid."));
        }

        if (result.InvalidEndDate)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "End date is invalid.", ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateResidentLeaseRequestDto.EndDate) : nameof(UpdateResidentLeaseRequestDto.EndDate)), result.ErrorMessage ?? "End date is invalid."));
        }

        if (result.DuplicateActiveLease)
        {
            return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? "An active lease for this resident already exists for the unit.", ApiErrorCodes.Conflict);
        }

        return CreateError(HttpStatusCode.BadRequest, result.ErrorMessage ?? (isCreate ? "Unable to create lease." : "Unable to update lease."), ApiErrorCodes.BusinessRuleViolation);
    }

    private static ResidentUnitLeaseDto MapLease(ManagementResidentLeaseListItem item)
    {
        return new ResidentUnitLeaseDto
        {
            LeaseId = item.LeaseId,
            ResidentId = item.ResidentId,
            UnitId = item.UnitId,
            PropertyId = item.PropertyId,
            PropertyName = item.PropertyName,
            PropertySlug = item.PropertySlug,
            UnitNr = item.UnitNr,
            UnitSlug = item.UnitSlug,
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

    private static ApiRouteContextDto CreateRouteContext(ResidentDashboardContext context)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = context.ResidentIdCode,
            ResidentDisplayName = context.FullName,
            CurrentSection = "resident-units"
        };
    }
}
