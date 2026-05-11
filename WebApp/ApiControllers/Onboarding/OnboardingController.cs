using App.BLL.Contracts;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.ManagementCompanies.Commands;
using App.BLL.DTO.Workspace.Models;
using App.BLL.DTO.Workspace.Queries;
using App.DTO.v1.Onboarding;
using App.DTO.v1.Shared;
using Asp.Versioning;
using Base.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers.Onboarding;

[ApiVersion("1.0")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v{version:apiVersion}/onboarding")]
public class OnboardingController : ApiControllerBase
{
    private readonly IAppBLL _bll;
    private readonly IBaseMapper<CreateManagementCompanyDto, ManagementCompanyBllDto> _managementCompanyMapper;

    public OnboardingController(
        IAppBLL bll,
        IBaseMapper<CreateManagementCompanyDto, ManagementCompanyBllDto> managementCompanyMapper)
    {
        _bll = bll;
        _managementCompanyMapper = managementCompanyMapper;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(OnboardingStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OnboardingStatusDto>> Status(CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var hasContext = await _bll.Workspaces.HasAnyContextAsync(appUserId.Value, cancellationToken);
        if (hasContext.IsFailed)
        {
            return ToApiError(hasContext.Errors);
        }

        string? defaultPath = null;
        var entryPoint = await _bll.Workspaces.ResolveWorkspaceEntryPointAsync(
            new ResolveWorkspaceEntryPointQuery
            {
                AppUserId = appUserId.Value
            },
            cancellationToken);

        if (entryPoint.IsFailed)
        {
            return ToApiError(entryPoint.Errors);
        }

        if (entryPoint.Value is not null)
        {
            defaultPath = entryPoint.Value.Kind switch
            {
                WorkspaceEntryPointKind.ManagementDashboard when !string.IsNullOrWhiteSpace(entryPoint.Value.CompanySlug)
                    => $"/companies/{entryPoint.Value.CompanySlug}",
                WorkspaceEntryPointKind.CustomerDashboard when !string.IsNullOrWhiteSpace(entryPoint.Value.CompanySlug)
                                                       && !string.IsNullOrWhiteSpace(entryPoint.Value.CustomerSlug)
                    => $"/companies/{entryPoint.Value.CompanySlug}/customers/{entryPoint.Value.CustomerSlug}",
                WorkspaceEntryPointKind.ResidentDashboard when !string.IsNullOrWhiteSpace(entryPoint.Value.CompanySlug)
                                                        && !string.IsNullOrWhiteSpace(entryPoint.Value.ResidentIdCode)
                    => $"/companies/{entryPoint.Value.CompanySlug}/residents/{entryPoint.Value.ResidentIdCode}",
                _ => null
            };
        }

        return Ok(new OnboardingStatusDto
        {
            HasWorkspaceContext = hasContext.Value,
            CreateManagementCompany = true,
            JoinManagementCompany = true,
            DefaultPath = defaultPath
        });
    }

    [HttpPost("management-companies")]
    [ProducesResponseType(typeof(CreatedManagementCompanyDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedManagementCompanyDto>> CreateManagementCompany(
        CreateManagementCompanyDto dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var bllDto = _managementCompanyMapper.Map(dto);
        if (bllDto is null)
        {
            return InvalidRequest("Management company payload is required.");
        }

        var result = await _bll.ManagementCompanies.CreateAsync(
            appUserId.Value,
            bllDto,
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        var created = new CreatedManagementCompanyDto
        {
            Id = result.Value.Id,
            Slug = result.Value.Slug,
            Name = result.Value.Name,
            Path = $"/companies/{result.Value.Slug}"
        };

        return Created(created.Path, created);
    }

    [HttpGet("management-company-roles")]
    [ProducesResponseType(typeof(IReadOnlyList<LookupOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LookupOptionDto>>> ManagementCompanyRoles(
        CancellationToken cancellationToken)
    {
        var result = await _bll.CompanyMemberships.GetAvailableRolesAsync(cancellationToken);
        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(result.Value
            .Select(role => new LookupOptionDto
            {
                Id = role.RoleId,
                Code = role.RoleCode,
                Label = role.RoleLabel
            })
            .ToList());
    }

    [HttpPost("management-company-join-requests")]
    [ProducesResponseType(typeof(JoinRequestResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<JoinRequestResultDto>> CreateJoinRequest(
        JoinManagementCompanyRequestDto dto,
        CancellationToken cancellationToken)
    {
        var appUserId = GetAppUserId();
        if (appUserId is null)
        {
            return UnauthorizedRequest("Authentication is required.");
        }

        var result = await _bll.CompanyMemberships.CreateJoinRequestAsync(
            new CreateCompanyJoinRequestCommand
            {
                AppUserId = appUserId.Value,
                RegistryCode = dto.RegistryCode,
                RequestedRoleId = dto.RequestedRoleId,
                Message = dto.Message
            },
            cancellationToken);

        if (result.IsFailed)
        {
            return ToApiError(result.Errors);
        }

        return Ok(new JoinRequestResultDto
        {
            Success = true,
            Message = "Join request submitted."
        });
    }
}
