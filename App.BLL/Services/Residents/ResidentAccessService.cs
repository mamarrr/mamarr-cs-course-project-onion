using App.BLL.Contracts.Residents;
using App.BLL.DTO.Common.Errors;
using App.BLL.DTO.Residents.Models;
using App.BLL.DTO.Residents.Queries;
using App.BLL.Mappers.Residents;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Residents;

public class ResidentAccessService : IResidentAccessService
{
    private static readonly HashSet<string> AllowedRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private readonly IAppUOW _uow;

    public ResidentAccessService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsAsync(
        GetResidentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(query.CompanySlug))
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(
            query.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError(App.Resources.Views.UiText.ManagementCompanyWasNotFound));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            query.UserId,
            company.Id,
            cancellationToken);

        if (roleCode is null || !AllowedRoleCodes.Contains(roleCode))
        {
            return Result.Fail(new ForbiddenError("Access denied."));
        }

        return Result.Ok(new CompanyResidentsModel
        {
            AppUserId = query.UserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        });
    }

    public async Task<Result<ResidentWorkspaceModel>> ResolveResidentWorkspaceAsync(
        GetResidentProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(query.CompanySlug))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(
            query.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        if (string.IsNullOrWhiteSpace(query.ResidentIdCode))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var resident = await _uow.Residents.FirstProfileAsync(
            query.CompanySlug,
            query.ResidentIdCode,
            cancellationToken);

        if (resident is null || resident.ManagementCompanyId != company.Id)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var roleCode = await _uow.ManagementCompanies.FindActiveUserRoleCodeAsync(
            query.UserId,
            company.Id,
            cancellationToken);
        if (roleCode is not null && AllowedRoleCodes.Contains(roleCode))
        {
            return Result.Ok(ResidentBllMapper.MapWorkspace(query.UserId, resident));
        }

        var hasResidentContext = await _uow.Residents.HasActiveUserResidentContextAsync(
            query.UserId,
            resident.Id,
            cancellationToken);

        return hasResidentContext
            ? Result.Ok(ResidentBllMapper.MapWorkspace(query.UserId, resident))
            : Result.Fail(new ForbiddenError("Access denied."));
    }
}
