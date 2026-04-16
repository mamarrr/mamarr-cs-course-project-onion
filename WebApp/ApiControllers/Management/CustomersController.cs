using System.Net;
using App.BLL.Management;
using App.DTO.v1;
using App.DTO.v1.Management;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Management;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu")]
public class CustomersController : ControllerBase
{
    private readonly IManagementCustomerAccessService _managementCustomerAccessService;
    private readonly IManagementCustomerService _managementCustomerService;

    public CustomersController(
        IManagementCustomerAccessService managementCustomerAccessService,
        IManagementCustomerService managementCustomerService)
    {
        _managementCustomerAccessService = managementCustomerAccessService;
        _managementCustomerService = managementCustomerService;
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
        var authorization = await AuthorizeCompanyAsync(companySlug, cancellationToken);
        if (authorization.ErrorResult != null)
        {
            return authorization.ErrorResult;
        }

        var result = await _managementCustomerService.ListAsync(authorization.Context!, cancellationToken);
        return Ok(new ManagementCustomersResponseDto
        {
            Customers = result.Customers.Select(x => new ManagementCustomerSummaryDto
            {
                CustomerId = x.CustomerId,
                CustomerSlug = x.CustomerSlug,
                Name = x.Name,
                RegistryCode = x.RegistryCode,
                BillingEmail = x.BillingEmail,
                BillingAddress = x.BillingAddress,
                Phone = x.Phone,
                RouteContext = CreateCustomerRouteContext(authorization.Context!, x.CustomerSlug, x.Name)
            }).ToList()
        });
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
        var authorization = await AuthorizeCompanyAsync(companySlug, cancellationToken);
        if (authorization.ErrorResult != null)
        {
            return authorization.ErrorResult;
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationError());
        }

        var result = await _managementCustomerService.CreateAsync(
            authorization.Context!,
            new ManagementCustomerCreateRequest
            {
                Name = dto.Name,
                RegistryCode = dto.RegistryCode,
                BillingEmail = dto.BillingEmail,
                BillingAddress = dto.BillingAddress,
                Phone = dto.Phone
            },
            cancellationToken);

        if (!result.Success || result.CreatedCustomerId == null)
        {
            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.ErrorMessage ?? "Unable to create customer.",
                result.DuplicateRegistryCode ? ApiErrorCodes.Duplicate : ApiErrorCodes.BusinessRuleViolation,
                result.DuplicateRegistryCode
                    ? (nameof(dto.RegistryCode), result.ErrorMessage ?? "Unable to create customer.")
                    : result.InvalidBillingEmail
                        ? (nameof(dto.BillingEmail), result.ErrorMessage ?? "Unable to create customer.")
                        : (string.Empty, result.ErrorMessage ?? "Unable to create customer.")));
        }

        var refreshedCustomers = await _managementCustomerService.ListAsync(authorization.Context!, cancellationToken);
        var createdCustomer = refreshedCustomers.Customers.FirstOrDefault(x => x.CustomerId == result.CreatedCustomerId.Value);
        if (createdCustomer == null)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, "Created customer could not be resolved.", ApiErrorCodes.BusinessRuleViolation));
        }

        var response = new CreateManagementCustomerResponseDto
        {
            CustomerId = createdCustomer.CustomerId,
            CustomerSlug = createdCustomer.CustomerSlug,
            RouteContext = CreateCustomerRouteContext(authorization.Context!, createdCustomer.CustomerSlug, createdCustomer.Name)
        };

        return CreatedAtAction(
            nameof(GetCustomers),
            new { version = "1.0", companySlug },
            response);
    }

    private async Task<(ManagementCustomersAuthorizedContext? Context, ActionResult? ErrorResult)> AuthorizeCompanyAsync(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var authorization = await _managementCustomerAccessService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
        if (authorization.CompanyNotFound)
        {
            return (null, NotFound(CreateError(HttpStatusCode.NotFound, "Management company was not found.", ApiErrorCodes.NotFound)));
        }

        if (authorization.IsForbidden || authorization.Context == null)
        {
            return (null, StatusCode((int)HttpStatusCode.Forbidden, CreateError(HttpStatusCode.Forbidden, "Access denied.", ApiErrorCodes.Forbidden)));
        }

        return (authorization.Context, null);
    }

    private Guid? GetAppUserId()
    {
        var userIdValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : null;
    }

    private ApiRouteContextDto CreateCustomerRouteContext(
        ManagementCustomersAuthorizedContext context,
        string customerSlug,
        string customerName)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            CustomerSlug = customerSlug,
            CustomerName = customerName,
            CurrentSection = "customer-dashboard"
        };
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

    private RestApiErrorResponse CreateError(HttpStatusCode status, string message, string code, params (string Key, string Message)[] details)
    {
        var errors = details
            .Where(x => !string.IsNullOrWhiteSpace(x.Message))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());

        return new RestApiErrorResponse
        {
            Status = status,
            Error = message,
            ErrorCode = code,
            Errors = errors,
            TraceId = HttpContext.TraceIdentifier
        };
    }
}
