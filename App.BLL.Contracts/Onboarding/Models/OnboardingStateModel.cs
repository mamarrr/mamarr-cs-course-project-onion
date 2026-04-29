namespace App.BLL.Contracts.Onboarding.Models;

public sealed class OnboardingStateModel
{
    public bool HasAnyContext { get; init; }
    public string? DefaultManagementCompanySlug { get; init; }
}
