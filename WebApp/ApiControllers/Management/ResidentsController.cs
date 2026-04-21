using System.Net;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Residents;
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
[Route("/api/v{version:apiVersion}/co/{companySlug}/re")]
public class ResidentsController : ProfileApiControllerBase
{
    private readonly IResidentAccessService _residentAccessService;
    private readonly ICompanyResidentService _companyResidentService;

    public ResidentsController(
        IResidentAccessService residentAccessService,
        ICompanyResidentService companyResidentService)
    {
        _residentAccessService = residentAccessService;
        _companyResidentService = companyResidentService;
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType<ManagementResidentsResponseDto>((int)HttpStatusCode.OK)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<ManagementResidentsResponseDto>> GetResidents(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var authorization = await AuthorizeCompanyAsync(companySlug, cancellationToken);
        if (authorization.ErrorResult != null)
        {
            return authorization.ErrorResult;
        }

        var result = await _companyResidentService.ListAsync(authorization.Context!, cancellationToken);
        return Ok(new ManagementResidentsResponseDto
        {
            Residents = result.Residents.Select(x => new ManagementResidentSummaryDto
            {
                ResidentId = x.ResidentId,
                FirstName = x.FirstName,
                LastName = x.LastName,
                FullName = x.FullName,
                IdCode = x.IdCode,
                PreferredLanguage = x.PreferredLanguage,
                IsActive = x.IsActive,
                RouteContext = CreateResidentRouteContext(authorization.Context!, x.IdCode, x.FullName)
            }).ToList()
        });
    }

    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType<CreateManagementResidentResponseDto>((int)HttpStatusCode.Created)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.Forbidden)]
    [ProducesResponseType<RestApiErrorResponse>((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CreateManagementResidentResponseDto>> CreateResident(
        string companySlug,
        [FromBody] CreateManagementResidentRequestDto dto,
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

        var result = await _companyResidentService.CreateAsync(
            authorization.Context!,
            new ResidentCreateRequest
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                IdCode = dto.IdCode,
                PreferredLanguage = dto.PreferredLanguage
            },
            cancellationToken);

        if (!result.Success || result.CreatedResidentId == null)
        {
            var detail = result.DuplicateIdCode
                ? (nameof(dto.IdCode), result.ErrorMessage ?? "Resident ID code already exists.")
                : result.InvalidFirstName
                    ? (nameof(dto.FirstName), result.ErrorMessage ?? "First name is invalid.")
                    : result.InvalidLastName
                        ? (nameof(dto.LastName), result.ErrorMessage ?? "Last name is invalid.")
                        : result.InvalidIdCode
                            ? (nameof(dto.IdCode), result.ErrorMessage ?? "ID code is invalid.")
                            : (string.Empty, result.ErrorMessage ?? "Unable to create resident.");

            return BadRequest(CreateError(
                HttpStatusCode.BadRequest,
                result.ErrorMessage ?? "Unable to create resident.",
                result.DuplicateIdCode ? ApiErrorCodes.Duplicate : ApiErrorCodes.BusinessRuleViolation,
                detail));
        }

        var refreshedResidents = await _companyResidentService.ListAsync(authorization.Context!, cancellationToken);
        var createdResident = refreshedResidents.Residents.FirstOrDefault(x => x.ResidentId == result.CreatedResidentId.Value);
        if (createdResident == null)
        {
            return BadRequest(CreateError(HttpStatusCode.BadRequest, "Created resident could not be resolved.", ApiErrorCodes.BusinessRuleViolation));
        }

        var response = new CreateManagementResidentResponseDto
        {
            ResidentId = createdResident.ResidentId,
            ResidentIdCode = createdResident.IdCode,
            RouteContext = CreateResidentRouteContext(authorization.Context!, createdResident.IdCode, createdResident.FullName)
        };

        return CreatedAtAction(
            nameof(GetResidents),
            new { version = "1.0", companySlug },
            response);
    }

    private async Task<(CompanyResidentsAuthorizedContext? Context, ActionResult? ErrorResult)> AuthorizeCompanyAsync(
        string companySlug,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId == null)
        {
            return (null, Unauthorized(CreateError(HttpStatusCode.Unauthorized, "Authentication is required.", ApiErrorCodes.Unauthorized)));
        }

        var authorization = await _residentAccessService.AuthorizeAsync(appUserId.Value, companySlug, cancellationToken);
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

    private static ApiRouteContextDto CreateResidentRouteContext(
        CompanyResidentsAuthorizedContext context,
        string residentIdCode,
        string residentDisplayName)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = context.CompanySlug,
            CompanyName = context.CompanyName,
            ResidentIdCode = residentIdCode,
            ResidentDisplayName = residentDisplayName,
            CurrentSection = "resident-dashboard"
        };
    }
}
