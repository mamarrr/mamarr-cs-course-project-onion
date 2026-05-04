using App.BLL.Contracts.Common.Errors;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Units.Models;
using App.BLL.Contracts.Units.Queries;
using App.BLL.Mappers.Units;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Units;

public class UnitAccessService : IUnitAccessService
{
    private readonly IAppUOW _uow;

    public UnitAccessService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<UnitWorkspaceModel>> ResolveUnitWorkspaceAsync(
        GetUnitDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return Result.Fail(new UnauthorizedError("Authentication is required."));
        }

        if (string.IsNullOrWhiteSpace(query.CompanySlug)
            || string.IsNullOrWhiteSpace(query.CustomerSlug)
            || string.IsNullOrWhiteSpace(query.PropertySlug)
            || string.IsNullOrWhiteSpace(query.UnitSlug))
        {
            return Result.Fail(new NotFoundError("Unit context was not found."));
        }

        var company = await _uow.ManagementCompanies.FirstBySlugAsync(
            query.CompanySlug,
            cancellationToken);
        if (company is null)
        {
            return Result.Fail(new NotFoundError("Unit context was not found."));
        }

        var roleCode = await _uow.Customers.FindActiveManagementCompanyRoleCodeAsync(
            company.Id,
            query.UserId,
            cancellationToken);
        if (roleCode is null)
        {
            return Result.Fail(new ForbiddenError(App.Resources.Views.UiText.AccessDeniedDescription));
        }

        var unit = await _uow.Units.FirstDashboardAsync(
            query.CompanySlug,
            query.CustomerSlug,
            query.PropertySlug,
            query.UnitSlug,
            cancellationToken);

        return unit is null
            ? Result.Fail(new NotFoundError("Unit context was not found."))
            : Result.Ok(UnitBllMapper.MapWorkspace(query.UserId, unit));
    }
}
