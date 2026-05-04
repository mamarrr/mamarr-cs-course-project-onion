using System.Net;
using App.BLL.Contracts.Units;
using App.DTO.v1;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Units;

namespace WebApp.ApiControllers.Property;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un")]
public class PropertyUnitsController : ControllerBase
{
    private readonly IUnitWorkspaceService _unitWorkspaceService;
    private readonly UnitApiMapper _unitMapper;

    public PropertyUnitsController(
        IUnitWorkspaceService unitWorkspaceService,
        UnitApiMapper unitMapper)
    {
        _unitWorkspaceService = unitWorkspaceService;
        _unitMapper = unitMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PropertyUnitsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyUnitsResponseDto>> GetUnits(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var result = await _unitWorkspaceService.GetPropertyUnitsAsync(
            _unitMapper.ToPropertyUnitsQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);

        return result.ToActionResult(_unitMapper.ToPropertyUnitsResponseDto);
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreatePropertyUnitResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreatePropertyUnitResponseDto>> CreateUnit(
        string companySlug,
        string customerSlug,
        string propertySlug,
        [FromBody] CreatePropertyUnitRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _unitWorkspaceService.CreateAsync(
            _unitMapper.ToCreateCommand(companySlug, customerSlug, propertySlug, dto, User),
            cancellationToken);

        if (result.IsFailed)
        {
            return result.ToActionResult(_unitMapper.ToCreateResponseDto);
        }

        return CreatedAtAction(
            nameof(GetUnits),
            new { version = "1.0", companySlug, customerSlug, propertySlug },
            _unitMapper.ToCreateResponseDto(result.Value));
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
