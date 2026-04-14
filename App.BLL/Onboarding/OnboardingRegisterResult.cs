namespace App.BLL.Onboarding;

public class OnboardingRegisterResult
{
    public bool Succeeded { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
}

