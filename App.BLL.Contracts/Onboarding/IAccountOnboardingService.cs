using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using App.BLL.Contracts.Onboarding.Queries;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IAccountOnboardingService
{
    Task<Result<CreateManagementCompanyModel>> CreateManagementCompanyAsync(
        CreateManagementCompanyCommand command,
        CancellationToken cancellationToken = default);

    Task<Result<OnboardingStateModel>> GetStateAsync(
        GetOnboardingStateQuery query,
        CancellationToken cancellationToken = default);

    Task<Result> CompleteAsync(
        CompleteAccountOnboardingCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyContextAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<string?> GetDefaultManagementCompanySlugAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> UserHasManagementCompanyAccessAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}