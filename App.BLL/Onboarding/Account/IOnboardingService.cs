namespace App.BLL.Onboarding.Account;

public interface IOnboardingService
{
    Task<OnboardingRegisterResult> RegisterAsync(OnboardingRegisterRequest request);
    Task<OnboardingLoginResult> LoginAsync(OnboardingLoginRequest request);
    Task<bool> HasAnyContextAsync(Guid appUserId);
    Task<OnboardingCreateManagementCompanyResult> CreateManagementCompanyAsync(OnboardingCreateManagementCompanyRequest request);
    Task<string?> GetDefaultManagementCompanySlugAsync(Guid appUserId);
    Task<bool> UserHasManagementCompanyAccessAsync(Guid appUserId, string companySlug);
    Task LogoutAsync();
}

