using App.BLL.Contracts.Common;
using App.BLL.Contracts.Onboarding;
using App.BLL.DTO.Common.Routes;
using App.DAL.Contracts;
using FluentResults;

namespace App.BLL.Services.Onboarding;

public class OnboardingService : IOnboardingService
{
    private readonly IAppUOW _uow;

    public OnboardingService(
        IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<bool>> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        var hasManagementContext = (await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
            appUserId,
            cancellationToken)).Count > 0;

        if (hasManagementContext)
        {
            return Result.Ok(true);
        }

        return Result.Ok(await _uow.Residents.HasActiveUserResidentContextAsync(appUserId, cancellationToken));
    }

    public async Task<Result<string?>> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        return Result.Ok<string?>((await _uow.ManagementCompanies.ActiveUserManagementContextsAsync(
                appUserId,
                cancellationToken))
            .Select(context => context.Slug)
            .FirstOrDefault());
    }

    public Task<Result<bool>> UserHasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(route.CompanySlug))
        {
            return Task.FromResult(Result.Ok(false));
        }

        return HasManagementCompanyAccessAsync(route, cancellationToken);
    }

    private async Task<Result<bool>> HasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken)
    {
        return Result.Ok(await _uow.ManagementCompanies.ActiveUserManagementContextExistsBySlugAsync(
            route.AppUserId,
            route.CompanySlug,
            cancellationToken));
    }
}
