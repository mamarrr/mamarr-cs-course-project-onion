namespace App.BLL.Contracts.Onboarding.Commands;

public sealed class CompleteAccountOnboardingCommand
{
    public Guid AppUserId { get; init; }
}
