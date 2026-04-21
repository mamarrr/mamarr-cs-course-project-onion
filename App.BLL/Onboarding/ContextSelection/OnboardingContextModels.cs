namespace App.BLL.Onboarding.ContextSelection;

public sealed class OnboardingContextSelectionCookieState
{
    public string? ContextType { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public string? CustomerId { get; init; }
}

public enum OnboardingContextRedirectDestination
{
    Home,
    ManagementDashboard,
    CustomerDashboard,
    ResidentDashboard
}

public sealed class OnboardingContextRedirectTarget
{
    public required OnboardingContextRedirectDestination Destination { get; init; }
    public string? CompanySlug { get; init; }
}

public sealed class OnboardingContextSelectionAuthorizationResult
{
    public bool Authorized { get; init; }
    public string? NormalizedType { get; init; }
    public Guid? ManagementCompanyId { get; init; }
    public string? ManagementCompanySlug { get; init; }
    public Guid? CustomerId { get; init; }
}
