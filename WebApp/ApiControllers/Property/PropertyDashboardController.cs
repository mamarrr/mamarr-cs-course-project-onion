using System.Net;
using App.BLL.Contracts.Properties.Services;
using App.DTO.v1;
using App.DTO.v1.Property;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Infrastructure.Results;
using WebApp.Mappers.Api.Properties;

namespace WebApp.ApiControllers.Property;

[ApiVersion("1.0")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("/api/v{version:apiVersion}/co/{companySlug}/cu/{customerSlug}/pr/{propertySlug}/dashboard")]
public class PropertyDashboardController : ControllerBase
{
    private readonly IPropertyWorkspaceService _propertyWorkspaceService;
    private readonly PropertyApiMapper _mapper;

    public PropertyDashboardController(
        IPropertyWorkspaceService propertyWorkspaceService,
        PropertyApiMapper mapper)
    {
        _propertyWorkspaceService = propertyWorkspaceService;
        _mapper = mapper;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<PropertyDashboardResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PropertyDashboardResponseDto>> GetDashboard(
        string companySlug,
        string customerSlug,
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var result = await _propertyWorkspaceService.GetDashboardAsync(
            _mapper.ToWorkspaceQuery(companySlug, customerSlug, propertySlug, User),
            cancellationToken);

        return result.ToActionResult(_mapper.ToDashboardResponseDto);
    }
}
