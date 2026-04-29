namespace App.BLL.Contracts.Onboarding.Queries;

public sealed class GetOnboardingStateQuery
{
    public Guid AppUserId { get; init; }
}
