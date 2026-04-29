using System.Net;
using App.BLL.Contracts.Customers.Services;
using App.BLL.Contracts.Properties.Services;
using App.DTO.v1;
using App.DTO.v1.Customer;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Customers;
using WebApp.Mappers.Api.Properties;

namespace WebApp.ApiControllers.Customer;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr")]
public class CustomerPropertiesController : ControllerBase
{
    private readonly ICustomerWorkspaceService _customerWorkspaceService;
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly CustomerWorkspaceApiMapper _customerMapper;
    private readonly PropertyApiMapper _propertyMapper;

    public CustomerPropertiesController(
        ICustomerWorkspaceService customerWorkspaceService,
        IPropertyWorkspaceService propertyWorkspaceService,
        CustomerWorkspaceApiMapper customerMapper,
        PropertyApiMapper propertyMapper)
    {
        _customerWorkspaceService = customerWorkspaceService;
        _propertyWorkspaceService = propertyWorkspaceService;
        _customerMapper = customerMapper;
        _propertyMapper = propertyMapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<CustomerPropertiesResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerPropertiesResponseDto>> GetProperties(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var customer = await _customerWorkspaceService.GetWorkspaceAsync(
            _customerMapper.ToQuery(companySlug, customerSlug, User),
            cancellationToken);
        if (customer.IsFailed)
        {
            return customer.ToActionResult(_ => new CustomerPropertiesResponseDto());
        }

        var properties = await _propertyWorkspaceService.GetCustomerPropertiesAsync(
            _propertyMapper.ToCustomerPropertiesQuery(companySlug, customerSlug, User),
            cancellationToken);
        if (properties.IsFailed)
        {
            return properties.ToActionResult(_ => new CustomerPropertiesResponseDto());
        }

        var propertyTypes = await _propertyWorkspaceService.GetPropertyTypeOptionsAsync(cancellationToken);
        if (propertyTypes.IsFailed)
        {
            return propertyTypes.ToActionResult(_ => new CustomerPropertiesResponseDto());
        }

        return Ok(_propertyMapper.ToCustomerPropertiesResponseDto(
            properties.Value,
            propertyTypes.Value,
            customer.Value.CompanySlug,
            customer.Value.CompanyName,
            customer.Value.CustomerSlug,
            customer.Value.CustomerName));
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateCustomerPropertyResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreateCustomerPropertyResponseDto>> CreateProperty(
        string companySlug,
        string customerSlug,
        [FromBody] CreateCustomerPropertyRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _propertyWorkspaceService.CreateAsync(
            _propertyMapper.ToCreateCommand(companySlug, customerSlug, dto, User),
            cancellationToken);
        if (result.IsFailed)
        {
            return result.ToActionResult(_ => new CreateCustomerPropertyResponseDto());
        }

        return CreatedAtAction(
            nameof(GetProperties),
            new { version = "1.0", companySlug, customerSlug },
            _propertyMapper.ToCreateResponseDto(result.Value));
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
