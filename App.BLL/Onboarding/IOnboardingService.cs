namespace App.BLL.Onboarding;

public interface IOnboardingService
{
    Task<OnboardingRegisterResult> RegisterAsync(OnboardingRegisterRequest request);
    Task<OnboardingLoginResult> LoginAsync(OnboardingLoginRequest request);
    Task LogoutAsync();
}

