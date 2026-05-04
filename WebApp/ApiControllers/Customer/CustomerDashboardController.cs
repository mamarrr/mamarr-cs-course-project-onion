using System.Net;
using App.BLL.Contracts.Customers;
using App.DTO.v1;
using App.DTO.v1.Customer;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Customers;

namespace WebApp.ApiControllers.Customer;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/dashboard")]
public class CustomerDashboardController : ControllerBase
{
    private readonly ICustomerWorkspaceService _customerWorkspaceService;
    private readonly CustomerWorkspaceApiMapper _mapper;

    public CustomerDashboardController(
        ICustomerWorkspaceService customerWorkspaceService,
        CustomerWorkspaceApiMapper mapper)
    {
        _customerWorkspaceService = customerWorkspaceService;
        _mapper = mapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<CustomerDashboardResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CustomerDashboardResponseDto>> GetDashboard(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken)
    {
        var query = _mapper.ToQuery(companySlug, customerSlug, User);
        var result = await _customerWorkspaceService.GetWorkspaceAsync(query, cancellationToken);

        return result.ToActionResult(_mapper.ToDashboardResponseDto);
    }
}
