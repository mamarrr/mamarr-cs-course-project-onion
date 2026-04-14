namespace App.BLL.Onboarding;

public class OnboardingCreateManagementCompanyResult
{
    public bool Succeeded { get; set; }
    public string[] Errors { get; set; } = [];
}
