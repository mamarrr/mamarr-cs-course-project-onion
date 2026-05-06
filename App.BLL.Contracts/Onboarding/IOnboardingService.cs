using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.Onboarding.Commands;
using App.BLL.DTO.Onboarding.Models;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IOnboardingService
{
    Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        Guid appUserId,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<OnboardingJoinRequestModel>> CreateJoinRequestAsync(
        CreateCompanyJoinRequestCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<OnboardingStateModel>> GetStateAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

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
