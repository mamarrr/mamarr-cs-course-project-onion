namespace App.BLL.Onboarding.Account;

public interface IAccountOnboardingService
{
    Task<AccountRegisterResult> RegisterAsync(AccountRegisterRequest request);
    Task<AccountLoginResult> LoginAsync(AccountLoginRequest request);
    Task<bool> HasAnyContextAsync(Guid appUserId);
    Task<CreateManagementCompanyResult> CreateManagementCompanyAsync(CreateManagementCompanyRequest request);
    Task<string?> GetDefaultManagementCompanySlugAsync(Guid appUserId);
    Task<bool> UserHasManagementCompanyAccessAsync(Guid appUserId, string companySlug);
    Task LogoutAsync();
}

