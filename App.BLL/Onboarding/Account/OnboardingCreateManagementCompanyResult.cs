namespace App.BLL.Onboarding.Account;

public class OnboardingCreateManagementCompanyResult
{
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = [];
    public Guid? ManagementCompanyId { get; set; }
    public string? ManagementCompanySlug { get; set; }
}
