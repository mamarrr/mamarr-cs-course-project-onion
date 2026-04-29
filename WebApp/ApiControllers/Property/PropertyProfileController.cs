using System.Net;
using App.BLL.Contracts.Properties.Services;
using App.DTO.v1;
using App.DTO.v1.Property;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Properties;

namespace WebApp.ApiControllers.Property;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/profile")]
public class PropertyProfileController : ProfileApiControllerBase
{
    private readonly IPropertyProfileService _propertyProfileService;
    private readonly PropertyApiMapper _mapper;

    public PropertyProfileController(
        IPropertyProfileService propertyProfileService,
        PropertyApiMapper mapper)
    {
        _propertyProfileService = propertyProfileService;
        _mapper = mapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PropertyProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyProfileResponseDto>> GetProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var result = await _propertyProfileService.GetAsync(
            _mapper.ToProfileQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);

        return result.ToActionResult(_mapper.ToProfileResponseDto);
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<PropertyProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyProfileResponseDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        string propertySlug,
        [FromBody] UpdatePropertyProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _propertyProfileService.UpdateAsync(
            _mapper.ToUpdateCommand(companySlug, customerSlug, propertySlug, dto, User),
            cancellationToken);

        return result.ToActionResult(_mapper.ToProfileResponseDto);
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
        [FromBody] DeletePropertyProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _propertyProfileService.DeleteAsync(
            _mapper.ToDeleteCommand(companySlug, customerSlug, propertySlug, dto, User),
            cancellationToken);

        return result.ToActionResult();
    }
}
