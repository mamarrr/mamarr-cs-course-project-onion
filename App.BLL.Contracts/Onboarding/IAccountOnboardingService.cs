using App.BLL.DTO.Onboarding.Commands;
using App.BLL.DTO.Onboarding.Models;
using App.BLL.DTO.Onboarding.Queries;
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