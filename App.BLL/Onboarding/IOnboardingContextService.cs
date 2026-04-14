namespace App.BLL.Onboarding;

public interface IOnboardingContextService
{
    Task<OnboardingContextRedirectTarget?> ResolveContextRedirectAsync(
        Guid appUserId,
        OnboardingContextSelectionCookieState cookieState,
        CancellationToken cancellationToken = default);

    Task<OnboardingContextSelectionAuthorizationResult> AuthorizeContextSelectionAsync(
        Guid appUserId,
        string contextType,
        Guid? contextId,
        CancellationToken cancellationToken = default);
}
