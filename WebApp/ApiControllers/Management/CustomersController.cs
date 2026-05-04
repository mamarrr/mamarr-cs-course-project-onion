using System.Net;
using App.BLL.Contracts.Customers;
using App.DTO.v1;
using App.DTO.v1.Management;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Customers;

namespace WebApp.ApiControllers.Management;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu")]
public class CustomersController : ControllerBase
{
    private readonly ICompanyCustomerService _companyCustomerService;
    private readonly CompanyCustomerApiMapper _mapper;

    public CustomersController(
        ICompanyCustomerService companyCustomerService,
        CompanyCustomerApiMapper mapper)
    {
        _companyCustomerService = companyCustomerService;
        _mapper = mapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ManagementCustomersResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ManagementCustomersResponseDto>> GetCustomers(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var query = _mapper.ToQuery(companySlug, User);
        var result = await _companyCustomerService.GetCompanyCustomersAsync(query, cancellationToken);

        return result.ToActionResult(_mapper.ToResponseDto);
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateManagementCustomerResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreateManagementCustomerResponseDto>> CreateCustomer(
        string companySlug,
        [FromBody] CreateManagementCustomerRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var command = _mapper.ToCommand(companySlug, dto, User);
        var result = await _companyCustomerService.CreateCustomerAsync(command, cancellationToken);
        if (result.IsFailed)
        {
            return result.ToActionResult(_mapper.ToCreateResponseDto);
        }

        return CreatedAtAction(
            nameof(GetCustomers),
            new { version = "1.0", companySlug },
            _mapper.ToCreateResponseDto(result.Value));
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
