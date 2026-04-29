using System.Net;
using App.BLL.Contracts.Units.Services;
using App.DTO.v1;
using App.DTO.v1.Shared;
using App.DTO.v1.Unit;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Units;

namespace WebApp.ApiControllers.Unit;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/un/{unitSlug}/profile")]
public class UnitProfileController : ProfileApiControllerBase
{
    private readonly IUnitProfileService _unitProfileService;
    private readonly UnitApiMapper _unitMapper;

    public UnitProfileController(
        IUnitProfileService unitProfileService,
        UnitApiMapper unitMapper)
    {
        _unitProfileService = unitProfileService;
        _unitMapper = unitMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<UnitProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitProfileResponseDto>> GetProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken)
    {
        var result = await _unitProfileService.GetAsync(
            _unitMapper.ToProfileQuery(companySlug, customerSlug, propertySlug, unitSlug, User),
            cancellationToken);

        return result.ToActionResult(_unitMapper.ToProfileResponseDto);
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<UnitProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<UnitProfileResponseDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromBody] UpdateUnitProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _unitProfileService.UpdateAsync(
            _unitMapper.ToUpdateCommand(companySlug, customerSlug, propertySlug, unitSlug, dto, User),
            cancellationToken);

        return result.ToActionResult(_unitMapper.ToProfileResponseDto);
    }

    [HttpDelete]
    [Consumes("application/json")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> DeleteProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        [FromBody] DeleteUnitProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _unitProfileService.DeleteAsync(
            _unitMapper.ToDeleteCommand(companySlug, customerSlug, propertySlug, unitSlug, dto, User),
            cancellationToken);

        return result.ToActionResult();
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
