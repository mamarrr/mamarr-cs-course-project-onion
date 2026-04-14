namespace App.BLL.Onboarding;

public interface IOnboardingService
{
    Task<OnboardingRegisterResult> RegisterAsync(OnboardingRegisterRequest request);
    Task<OnboardingLoginResult> LoginAsync(OnboardingLoginRequest request);
    Task<bool> HasAnyContextAsync(Guid appUserId);
    Task<OnboardingCreateManagementCompanyResult> CreateManagementCompanyAsync(OnboardingCreateManagementCompanyRequest request);
    Task LogoutAsync();
}

