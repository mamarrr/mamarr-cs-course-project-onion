using System.Net;
using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.Leases.Commands;
using App.BLL.Contracts.Leases.Queries;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Units.Models;
using App.BLL.Mappers.Leases;
using App.DTO.v1;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;
using Asp.Versioning;
using FluentResults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Leases;
using WebApp.Mappers.Api.Units;

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}")]
public class UnitTenantsController : ProfileApiControllerBase
{
    private readonly IUnitAccessService _unitAccessService;
    private readonly ILeaseAssignmentService _leaseAssignmentService;
    private readonly ILeaseLookupService _leaseLookupService;
    private readonly UnitApiMapper _unitMapper;
    private readonly LeaseApiMapper _leaseMapper;

    public UnitTenantsController(
        IUnitAccessService unitAccessService,
        ILeaseAssignmentService leaseAssignmentService,
        ILeaseLookupService leaseLookupService,
        UnitApiMapper unitMapper,
        LeaseApiMapper leaseMapper)
    {
        _unitAccessService = unitAccessService;
        _leaseAssignmentService = leaseAssignmentService;
        _leaseLookupService = leaseLookupService;
        _unitMapper = unitMapper;
        _leaseMapper = leaseMapper;
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

        var context = access.Context!;
        var leases = await _leaseAssignmentService.ListForUnitAsync(
            LeaseBllMapper.ToUnitLeasesQuery(context),
            cancellationToken);
        var leaseRoles = await _leaseLookupService.ListLeaseRolesAsync(cancellationToken);

        return Ok(new UnitTenantsBootstrapResponseDto
        {
            RouteContext = CreateRouteContext(context),
            Leases = leases.Value.Leases.Select(_leaseMapper.ToUnitLeaseDto).ToList(),
            LeaseRoles = leaseRoles.Value.Roles.Select(_leaseMapper.ToLookupOptionDto).ToList()
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

        var result = await _leaseLookupService.SearchResidentsAsync(
            ToSearchResidentsQuery(access.Context!, searchTerm),
            cancellationToken);

        return Ok(new UnitResidentSearchResponseDto
        {
            Residents = result.Value.Residents.Select(x => new UnitResidentSearchResultDto
            {
                ResidentId = x.ResidentId,
                FullName = x.FullName,
                IdCode = x.IdCode,
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
            ToCreateCommand(access.Context!, dto),
            cancellationToken);

        if (result.IsFailed)
        {
            return BadRequest(CreateLeaseCommandError(result.Errors, true));
        }

        return CreatedAtAction(
            nameof(GetTenants),
            new { version = "1.0", companySlug, customerSlug, propertySlug, unitSlug },
            new UnitLeaseCommandResponseDto { LeaseId = result.Value.LeaseId });
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

        return Ok(new UnitLeaseCommandResponseDto { LeaseId = result.Value.LeaseId });
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

    private async Task<(UnitWorkspaceModel? Context, ActionResult? ErrorResult)> ResolveUnitContextAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var unitAccess = await _unitAccessService.ResolveUnitWorkspaceAsync(
            _unitMapper.ToDashboardQuery(companySlug, customerSlug, propertySlug, unitSlug, User),
            cancellationToken);
        if (unitAccess.IsFailed)
        {
            return (null, unitAccess.ToActionResult(_ => new UnitTenantsBootstrapResponseDto()).Result);
        }

        return (unitAccess.Value, null);
    }

    private RestApiErrorResponse CreateLeaseCommandError(IReadOnlyList<IError> errors, bool isCreate)
    {
        var validation = errors.OfType<ValidationAppError>().FirstOrDefault();
        var failure = validation?.Failures.FirstOrDefault();
        if (failure?.PropertyName == "ResidentId")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.NotFound, (nameof(CreateUnitLeaseRequestDto.ResidentId), failure.ErrorMessage));
        }

        if (failure?.PropertyName == "LeaseRoleId")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateUnitLeaseRequestDto.LeaseRoleId) : nameof(UpdateUnitLeaseRequestDto.LeaseRoleId)), failure.ErrorMessage));
        }

        if (failure?.PropertyName == "StartDate")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateUnitLeaseRequestDto.StartDate) : nameof(UpdateUnitLeaseRequestDto.StartDate)), failure.ErrorMessage));
        }

        if (failure?.PropertyName == "EndDate")
        {
            return CreateError(HttpStatusCode.BadRequest, failure.ErrorMessage, ApiErrorCodes.ValidationFailed, ((isCreate ? nameof(CreateUnitLeaseRequestDto.EndDate) : nameof(UpdateUnitLeaseRequestDto.EndDate)), failure.ErrorMessage));
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

    private static CreateLeaseFromUnitCommand ToCreateCommand(
        UnitWorkspaceModel context,
        CreateUnitLeaseRequestDto dto)
    {
        return new CreateLeaseFromUnitCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            ResidentId = dto.ResidentId!.Value,
            LeaseRoleId = dto.LeaseRoleId!.Value,
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate,
            Notes = dto.Notes
        };
    }

    private static UpdateLeaseFromUnitCommand ToUpdateCommand(
        UnitWorkspaceModel context,
        Guid leaseId,
        UpdateUnitLeaseRequestDto dto)
    {
        return new UpdateLeaseFromUnitCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            LeaseId = leaseId,
            LeaseRoleId = dto.LeaseRoleId!.Value,
            StartDate = dto.StartDate!.Value,
            EndDate = dto.EndDate,
            Notes = dto.Notes
        };
    }

    private static DeleteLeaseFromUnitCommand ToDeleteCommand(
        UnitWorkspaceModel context,
        Guid leaseId)
    {
        return new DeleteLeaseFromUnitCommand
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            LeaseId = leaseId
        };
    }

    private static SearchLeaseResidentsQuery ToSearchResidentsQuery(
        UnitWorkspaceModel context,
        string? searchTerm)
    {
        return new SearchLeaseResidentsQuery
        {
            AppUserId = context.AppUserId,
            ManagementCompanyId = context.ManagementCompanyId,
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerId = context.CustomerId,
            CustomerSlug = context.CustomerSlug,
            CustomerName = context.CustomerName,
            PropertyId = context.PropertyId,
            PropertySlug = context.PropertySlug,
            PropertyName = context.PropertyName,
            UnitId = context.UnitId,
            UnitSlug = context.UnitSlug,
            UnitNr = context.UnitNr,
            SearchTerm = searchTerm
        };
    }

    private static ApiRouteContextDto CreateRouteContext(UnitWorkspaceModel context)
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
