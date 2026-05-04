using System.Net;
using App.BLL.Contracts.Residents;
using App.DTO.v1;
using App.DTO.v1.Management;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Residents;

namespace WebApp.ApiControllers.Management;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/re")]
public class ResidentsController : ProfileApiControllerBase
{
    private readonly IResidentWorkspaceService _residentWorkspaceService;
    private readonly ResidentApiMapper _residentMapper;

    public ResidentsController(
        IResidentWorkspaceService residentWorkspaceService,
        ResidentApiMapper residentMapper)
    {
        _residentWorkspaceService = residentWorkspaceService;
        _residentMapper = residentMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ManagementResidentsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ManagementResidentsResponseDto>> GetResidents(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var result = await _residentWorkspaceService.GetResidentsAsync(
            _residentMapper.ToResidentsQuery(companySlug, User),
            cancellationToken);

        return result.ToActionResult(_residentMapper.ToResidentsResponseDto);
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateManagementResidentResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreateManagementResidentResponseDto>> CreateResident(
        string companySlug,
        [FromBody] CreateManagementResidentRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await _residentWorkspaceService.GetResidentsAsync(
            _residentMapper.ToResidentsQuery(companySlug, User),
            cancellationToken);
        if (access.IsFailed)
        {
            return access.ToActionResult(_ => new CreateManagementResidentResponseDto());
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _residentWorkspaceService.CreateAsync(
            _residentMapper.ToCreateCommand(companySlug, dto, User),
            cancellationToken);

        if (result.IsFailed)
        {
            return result.ToActionResult(_residentMapper.ToCreateResponseDto);
        }

        return CreatedAtAction(
            nameof(GetResidents),
            new { version = "1.0", companySlug },
            _residentMapper.ToCreateResponseDto(result.Value));
    }

    private RestApiErrorResponse CreateValidationError()
    {
        return new RestApiErrorResponse
        {
            Status = HttpStatusCode.BadRequest,
            Error = "Validation failed.",
            ErrorCode = ApiErrorCodes.ValidationFailed,
            Errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray()),
            TraceId = HttpContext.TraceIdentifier
        };
    }
}
