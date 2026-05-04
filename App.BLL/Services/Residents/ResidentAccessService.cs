using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Residents.Models;
using App.BLL.Contracts.Residents.Queries;
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
        var company = await ResolveCompanyResidentsAsync(
            new GetResidentsQuery
            {
                UserId = query.UserId,
                CompanySlug = query.CompanySlug
            },
            cancellationToken);
        if (company.IsFailed)
        {
            if (company.Errors.OfType<NotFoundError>().Any())
            {
                return Result.Fail(new NotFoundError("Resident context was not found."));
            }

            return Result.Fail(company.Errors);
        }

        if (string.IsNullOrWhiteSpace(query.ResidentIdCode))
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        var resident = await _uow.Residents.FirstProfileAsync(
            query.CompanySlug,
            query.ResidentIdCode,
            cancellationToken);

        if (resident is null || resident.ManagementCompanyId != company.Value.ManagementCompanyId)
        {
            return Result.Fail(new NotFoundError("Resident context was not found."));
        }

        return Result.Ok(ResidentBllMapper.MapWorkspace(query.UserId, resident));
    }
}
