using System.Net;
using App.BLL.Contracts.Residents.Services;
using App.DTO.v1;
using App.DTO.v1.Resident;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Residents;

namespace WebApp.ApiControllers.Resident;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/re/{residentIdCode}/profile")]
public class ResidentProfileController : ProfileApiControllerBase
{
    private readonly IResidentProfileService _residentProfileService;
    private readonly ResidentApiMapper _residentMapper;

    public ResidentProfileController(
        IResidentProfileService residentProfileService,
        ResidentApiMapper residentMapper)
    {
        _residentProfileService = residentProfileService;
        _residentMapper = residentMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ResidentProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentProfileResponseDto>> GetProfile(
        string companySlug,
        string residentIdCode,
        CancellationToken cancellationToken)
    {
        var result = await _residentProfileService.GetAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);

        return result.ToActionResult(_residentMapper.ToProfileResponseDto);
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<ResidentProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ResidentProfileResponseDto>> UpdateProfile(
        string companySlug,
        string residentIdCode,
        [FromBody] UpdateResidentProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await _residentProfileService.GetAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);
        if (access.IsFailed)
        {
            return access.ToActionResult(_residentMapper.ToProfileResponseDto);
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _residentProfileService.UpdateAsync(
            _residentMapper.ToUpdateCommand(companySlug, residentIdCode, dto, User),
            cancellationToken);

        return result.ToActionResult(_residentMapper.ToProfileResponseDto);
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
        string residentIdCode,
        [FromBody] DeleteResidentProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        var access = await _residentProfileService.GetAsync(
            _residentMapper.ToResidentQuery(companySlug, residentIdCode, User),
            cancellationToken);
        if (access.IsFailed)
        {
            return access.ToActionResult(_residentMapper.ToProfileResponseDto).Result!;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _residentProfileService.DeleteAsync(
            _residentMapper.ToDeleteCommand(companySlug, residentIdCode, dto, User),
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
