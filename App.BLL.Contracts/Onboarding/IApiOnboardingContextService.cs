using App.BLL.Contracts.Onboarding.Models;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IApiOnboardingContextService
{
    Task<Result<ApiOnboardingContextCatalogModel>> GetContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);
}
