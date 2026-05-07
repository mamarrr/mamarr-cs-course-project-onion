using App.BLL.DTO.Common.Routes;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IOnboardingService
{
    Task<Result<bool>> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result<string?>> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> UserHasManagementCompanyAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);
}
