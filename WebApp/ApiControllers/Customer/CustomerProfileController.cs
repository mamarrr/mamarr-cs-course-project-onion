using System.Net;
using App.BLL.Contracts.Customers;
using App.DTO.v1;
using App.DTO.v1.Customer;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ApiControllers.Management;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Customers;

namespace WebApp.ApiControllers.Customer;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/profile")]
public class CustomerProfileController : ProfileApiControllerBase
{
    private readonly CustomerProfileApiMapper _mapper;
    private readonly ICustomerProfileService _customerProfileService;

    public CustomerProfileController(
        CustomerProfileApiMapper mapper,
        ICustomerProfileService customerProfileService)
    {
        _mapper = mapper;
        _customerProfileService = customerProfileService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<CustomerProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerProfileResponseDto>> GetProfile(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var query = _mapper.ToQuery(companySlug, customerSlug, User);
        var result = await _customerProfileService.GetAsync(query, cancellationToken);

        return result.ToActionResult(_mapper.ToResponseDto);
    }

    [HttpPut]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<CustomerProfileResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerProfileResponseDto>> UpdateProfile(
        string companySlug,
        string customerSlug,
        [FromBody] UpdateCustomerProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var command = _mapper.ToCommand(companySlug, customerSlug, dto, User);
        var result = await _customerProfileService.UpdateAsync(command, cancellationToken);

        return result.ToActionResult(_mapper.ToResponseDto);
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
        [FromBody] DeleteCustomerProfileRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var command = _mapper.ToCommand(companySlug, customerSlug, dto, User);
        var result = await _customerProfileService.DeleteAsync(command, cancellationToken);

        return result.ToActionResult();
    }
}
