using App.BLL.Contracts.Onboarding.Commands;
using App.BLL.Contracts.Onboarding.Models;
using FluentResults;

namespace App.BLL.Contracts.Onboarding;

public interface IOnboardingCompanyJoinRequestService
{
    Task<Result<OnboardingJoinRequestModel>> CreateJoinRequestAsync(
        CreateCompanyJoinRequestCommand command,
        CancellationToken cancellationToken = default);
}
