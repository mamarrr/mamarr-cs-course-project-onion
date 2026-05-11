using App.BLL.Contracts;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Leases;
using App.DTO.v1.Common;
using App.DTO.v1.Mappers.Portal.Leases;
using App.DTO.v1.Portal.Leases;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Portal;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/portal/companies/{companySlug}")]
public class LeasesController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CreateResidentLeaseDto, LeaseBllDto> _createResidentLeaseMapper;
    private readonly IBaseMapper<CreateUnitLeaseDto, LeaseBllDto> _createUnitLeaseMapper;
    private readonly IBaseMapper<UpdateLeaseDto, LeaseBllDto> _updateLeaseMapper;
    private readonly LeaseResponseApiMapper _responseMapper;

    public LeasesController(IAppBLL bll)
    {
        _bll = bll;

        var commandMapper = new LeaseApiMapper();
        _createResidentLeaseMapper = commandMapper;
        _createUnitLeaseMapper = commandMapper;
        _updateLeaseMapper = commandMapper;
        _responseMapper = new LeaseResponseApiMapper();
    }

    [HttpGet("residents/{residentIdCode}/leases")]
    [ProducesResponseType(typeof(IReadOnlyList<ResidentLeaseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResidentLeaseListItemDto>>> ListForResident(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.ListForResidentAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var leases = result.Value.Leases
            .Select(lease => _responseMapper.MapResidentLease(lease, companySlug, residentIdCode))
            .ToList();

        return Ok(leases);
    }

    [HttpGet("residents/{residentIdCode}/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseDto>> GetForResident(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.GetForResidentAsync(
            ToResidentLeaseRoute(companySlug, residentIdCode, leaseId, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.MapForResident(result.Value, companySlug, residentIdCode));
    }

    [HttpPost("residents/{residentIdCode}/leases")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LeaseDto>> CreateForResident(
        string companySlug,
        string residentIdCode,
        CreateResidentLeaseDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _createResidentLeaseMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Lease payload is required.");
        }

        var result = await _bll.Leases.CreateForResidentAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var lease = _responseMapper.MapForResident(result.Value, companySlug, residentIdCode);
        return Created(lease.Path, lease);
    }

    [HttpPut("residents/{residentIdCode}/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseDto>> UpdateFromResident(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        UpdateLeaseDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateLeaseMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Lease payload is required.");
        }

        var result = await _bll.Leases.UpdateFromResidentAsync(
            ToResidentLeaseRoute(companySlug, residentIdCode, leaseId, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.MapForResident(result.Value, companySlug, residentIdCode));
    }

    [HttpDelete("residents/{residentIdCode}/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> DeleteFromResident(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.DeleteFromResidentAsync(
            ToResidentLeaseRoute(companySlug, residentIdCode, leaseId, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Lease deleted."
        });
    }

    [HttpGet("residents/{residentIdCode}/leases/property-search")]
    [ProducesResponseType(typeof(LeasePropertySearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeasePropertySearchResultDto>> SearchProperties(
        string companySlug,
        string residentIdCode,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.SearchPropertiesAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            searchTerm,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.Map(result.Value));
    }

    [HttpGet("residents/{residentIdCode}/leases/properties/{propertyId:guid}/units")]
    [ProducesResponseType(typeof(LeaseUnitOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseUnitOptionsDto>> ListUnitsForProperty(
        string companySlug,
        string residentIdCode,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.ListUnitsForPropertyAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            propertyId,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.Map(result.Value));
    }

    [HttpGet("residents/{residentIdCode}/leases/roles")]
    [ProducesResponseType(typeof(LeaseRoleOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseRoleOptionsDto>> ListLeaseRolesForResident(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var access = await _bll.Leases.ListForResidentAsync(
            ToResidentRoute(companySlug, residentIdCode, appUserId.Value),
            cancellationToken);
        if (access.IsFailed)
        {
            return ToApiError(access.Errors);
        }

        var result = await _bll.Leases.ListLeaseRolesAsync(cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases")]
    [ProducesResponseType(typeof(IReadOnlyList<UnitLeaseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UnitLeaseListItemDto>>> ListForUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.ListForUnitAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var leases = result.Value.Leases
            .Select(lease => _responseMapper.MapUnitLease(
                lease,
                companySlug,
                customerSlug,
                propertySlug,
                unitSlug))
            .ToList();

        return Ok(leases);
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseDto>> GetForUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.GetForUnitAsync(
            ToUnitLeaseRoute(companySlug, customerSlug, propertySlug, unitSlug, leaseId, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.MapForUnit(result.Value, companySlug, customerSlug, propertySlug, unitSlug));
    }

    [HttpPost("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LeaseDto>> CreateForUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CreateUnitLeaseDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _createUnitLeaseMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Lease payload is required.");
        }

        var result = await _bll.Leases.CreateForUnitAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var lease = _responseMapper.MapForUnit(result.Value, companySlug, customerSlug, propertySlug, unitSlug);
        return Created(lease.Path, lease);
    }

    [HttpPut("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(LeaseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseDto>> UpdateFromUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        UpdateLeaseDto? dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _updateLeaseMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Lease payload is required.");
        }

        var result = await _bll.Leases.UpdateFromUnitAsync(
            ToUnitLeaseRoute(companySlug, customerSlug, propertySlug, unitSlug, leaseId, appUserId.Value),
            bllDto,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.MapForUnit(result.Value, companySlug, customerSlug, propertySlug, unitSlug));
    }

    [HttpDelete("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/{leaseId:guid}")]
    [ProducesResponseType(typeof(CommandResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResultDto>> DeleteFromUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.DeleteFromUnitAsync(
            ToUnitLeaseRoute(companySlug, customerSlug, propertySlug, unitSlug, leaseId, appUserId.Value),
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new CommandResultDto
        {
            Success = true,
            Message = "Lease deleted."
        });
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/resident-search")]
    [ProducesResponseType(typeof(LeaseResidentSearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseResidentSearchResultDto>> SearchResidents(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.Leases.SearchResidentsAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            searchTerm,
            cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.Map(result.Value));
    }

    [HttpGet("customers/{customerSlug}/properties/{propertySlug}/units/{unitSlug}/leases/roles")]
    [ProducesResponseType(typeof(LeaseRoleOptionsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LeaseRoleOptionsDto>> ListLeaseRolesForUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var access = await _bll.Leases.ListForUnitAsync(
            ToUnitRoute(companySlug, customerSlug, propertySlug, unitSlug, appUserId.Value),
            cancellationToken);
        if (access.IsFailed)
        {
            return ToApiError(access.Errors);
        }

        var result = await _bll.Leases.ListLeaseRolesAsync(cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(_responseMapper.Map(result.Value));
    }

    private static ResidentRoute ToResidentRoute(
        string companySlug,
        string residentIdCode,
        Guid appUserId)
    {
        return new ResidentRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode
        };
    }

    private static ResidentLeaseRoute ToResidentLeaseRoute(
        string companySlug,
        string residentIdCode,
        Guid leaseId,
        Guid appUserId)
    {
        return new ResidentLeaseRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            ResidentIdCode = residentIdCode,
            LeaseId = leaseId
        };
    }

    private static UnitRoute ToUnitRoute(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid appUserId)
    {
        return new UnitRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug
        };
    }

    private static UnitLeaseRoute ToUnitLeaseRoute(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        Guid leaseId,
        Guid appUserId)
    {
        return new UnitLeaseRoute
        {
            AppUserId = appUserId,
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UnitSlug = unitSlug,
            LeaseId = leaseId
        };
    }
}
