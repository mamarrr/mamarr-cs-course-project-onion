using System.Net;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Leases.Commands;
using App.BLL.Contracts.Leases.Queries;
using App.BLL.Contracts.Leases.Services;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
using App.BLL.Contracts.Residents.Services;
using App.BLL.Mappers.Leases;
using App.DTO.v1;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;
using Asp.Versioning;
using FluentResults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Mappers.Api.Leases;

namespace WebApp.ApiControllers.Resident;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/re/{residentIdCode}")]
public class ResidentUnitsController : ProfileApiControllerBase
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly ILeaseAssignmentService _leaseAssignmentService;
    private readonly ILeaseLookupService _leaseLookupService;
    private readonly LeaseApiMapper _leaseMapper;

    public ResidentUnitsController(
        IResidentAccessService residentAccessService,
        ILeaseAssignmentService leaseAssignmentService,
        ILeaseLookupService leaseLookupService,
        LeaseApiMapper leaseMapper)
    {
        _residentAccessService = residentAccessService;
        _leaseAssignmentService = leaseAssignmentService;
        _leaseLookupService = leaseLookupService;
        _leaseMapper = leaseMapper;
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

        var context = access.Context!;
        var leases = await _leaseAssignmentService.ListForResidentAsync(
            LeaseBllMapper.ToResidentLeasesQuery(context),
            cancellationToken);
        var leaseRoles = await _leaseLookupService.ListLeaseRolesAsync(cancellationToken);

        return Ok(new ResidentUnitsBootstrapResponseDto
        {
            RouteContext = CreateRouteContext(context),
            Leases = leases.Value.Leases.Select(_leaseMapper.ToResidentLeaseDto).ToList(),
            LeaseRoles = leaseRoles.Value.Roles.Select(_leaseMapper.ToLookupOptionDto).ToList()
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

        var result = await _leaseLookupService.SearchPropertiesAsync(
            ToSearchPropertiesQuery(access.Context!, searchTerm),
            cancellationToken);

        return Ok(new ResidentPropertySearchResponseDto
        {
            Properties = result.Value.Properties.Select(x => new ResidentPropertySearchResultDto
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

        var result = await _leaseLookupService.ListUnitsForPropertyAsync(
            ToUnitsForPropertyQuery(access.Context!, propertyId),
            cancellationToken);
        if (result.IsFailed)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Property was not found.";
            return NotFound(CreateError(HttpStatusCode.NotFound, message, ApiErrorCodes.NotFound));
        }

        return Ok(new ResidentPropertyUnitsResponseDto
        {
            Units = result.Value.Units.Select(x => new ResidentPropertyUnitOptionDto
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

        var result = await _leaseAssignmentService.CreateFromResidentAsync(
            ToCreateCommand(access.Context!, dto),
            cancellationToken);

        if (result.IsFailed)
        {
            return BadRequest(CreateLeaseCommandError(result.Errors, true));
        }

        return CreatedAtAction(
            nameof(GetUnits),
            new { version = "1.0", companySlug, residentIdCode },
            new ResidentLeaseCommandResponseDto { LeaseId = result.Value.LeaseId });
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

        var result = await _leaseAssignmentService.UpdateFromResidentAsync(
            ToUpdateCommand(access.Context!, leaseId, dto),
            cancellationToken);

        if (IsNotFound(result.Errors))
        {
            var message = result.Errors.First().Message;
            return NotFound(CreateError(HttpStatusCode.NotFound, message, ApiErrorCodes.NotFound));
        }

        if (result.IsFailed)
        {
            return BadRequest(CreateLeaseCommandError(result.Errors, false));
        }

        return Ok(new ResidentLeaseCommandResponseDto { LeaseId = result.Value.LeaseId });
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

        var result = await _leaseAssignmentService.DeleteFromResidentAsync(
            ToDeleteCommand(access.Context!, leaseId),
            cancellationToken);

        if (IsNotFound(result.Errors))
        {
            var message = result.Errors.First().Message;
            return NotFound(CreateError(HttpStatusCode.NotFound, message, ApiErrorCodes.NotFound));
        }

        if (result.IsFailed)
        {
            var message = result.Errors.FirstOrDefault()?.Message ?? "Unable to delete lease.";
            return BadRequest(CreateError(HttpStatusCode.BadRequest, message, ApiErrorCodes.BusinessRuleViolation));
        }

        return NoContent();
    }

    private async Task<(ResidentWorkspaceModel? Context, ActionResult? ErrorResult)> ResolveResidentContextAsync(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var access = await _residentAccessService.ResolveResidentWorkspaceAsync(
            new GetResidentProfileQuery
            {
                UserId = appUserId.Value,
                CompanySlug = companySlug,
                ResidentIdCode = residentIdCode
            },
            cancellationToken);

        if (access.Errors.OfType<NotFoundError>().Any())
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Resident context was not found.", ApiErrorCodes.NotFound)));
        }

        if (access.Errors.OfType<ForbiddenError>().Any())
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        if (access.IsFailed)
        {
            return (null, BadRequest(CreateError(HttpStatusCode.BadRequest, access.Errors.First().Message, ApiErrorCodes.BusinessRuleViolation)));
        }

        return (access.Value, null);
    }

    private RestApiErrorResponse CreateLeaseCommandError(IReadOnlyList<IError> errors, bool isCreate)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        var failure = validation?.Failures.FirstOrDefault();
        if (failure?.PropertyName == "UnitId")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.NotFound, ((isCreate ? nameof(CreateResidentLeaseRequestDto.UnitId) : string.Empty), failure.ErrorMessage));
        }

        if (failure?.PropertyName == "LeaseRoleId")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateResidentLeaseRequestDto.LeaseRoleId) : nameof(UpdateResidentLeaseRequestDto.LeaseRoleId)), failure.ErrorMessage));
        }

        if (failure?.PropertyName == "StartDate")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateResidentLeaseRequestDto.StartDate) : nameof(UpdateResidentLeaseRequestDto.StartDate)), failure.ErrorMessage));
        }

        if (failure?.PropertyName == "EndDate")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateResidentLeaseRequestDto.EndDate) : nameof(UpdateResidentLeaseRequestDto.EndDate)), failure.ErrorMessage));
        }

        var conflict = errors.OfType<ConflictError>().FirstOrDefault();
        if (conflict is not null)
        {
            return CreateError(HttpStatusCode.BadRequest, conflict.Message, ApiErrorCodes.Conflict);
        }

        var message = errors.FirstOrDefault()?.Message ?? (isCreate ? "Unable to create lease." : "Unable to update lease.");
        return CreateError(HttpStatusCode.BadRequest, message, ApiErrorCodes.BusinessRuleViolation);
    }

    private static bool IsNotFound(IReadOnlyList<IError> errors)
        => errors.OfType<NotFoundError>().Any();

    private static CreateLeaseFromResidentCommand ToCreateCommand(
        ResidentWorkspaceModel context,
        CreateResidentLeaseRequestDto dto)
    {
        return new CreateLeaseFromResidentCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            UnitId = dto.UnitId!.Value,
            LeaseRoleId = dto.LeaseRoleId!.Value,
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            Notes = dto.Notes
        };
    }

    private static UpdateLeaseFromResidentCommand ToUpdateCommand(
        ResidentWorkspaceModel context,
        Guid leaseId,
        UpdateResidentLeaseRequestDto dto)
    {
        return new UpdateLeaseFromResidentCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            LeaseId = leaseId,
            LeaseRoleId = dto.LeaseRoleId!.Value,
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            Notes = dto.Notes
        };
    }

    private static DeleteLeaseFromResidentCommand ToDeleteCommand(
        ResidentWorkspaceModel context,
        Guid leaseId)
    {
        return new DeleteLeaseFromResidentCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            LeaseId = leaseId
        };
    }

    private static SearchLeasePropertiesQuery ToSearchPropertiesQuery(
        ResidentWorkspaceModel context,
        string? searchTerm)
    {
        return new SearchLeasePropertiesQuery
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            SearchTerm = searchTerm
        };
    }

    private static GetLeaseUnitsForPropertyQuery ToUnitsForPropertyQuery(
        ResidentWorkspaceModel context,
        Guid propertyId)
    {
        return new GetLeaseUnitsForPropertyQuery
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentId = context.ResidentId,
            ResidentIdCode = context.ResidentIdCode,
            FullName = context.FullName,
            PropertyId = propertyId
        };
    }

    private static ApiRouteContextDto CreateRouteContext(ResidentWorkspaceModel context)
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
