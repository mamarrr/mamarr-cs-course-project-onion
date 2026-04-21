namespace App.BLL.Onboarding.Account;

public class OnboardingLoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public bool RememberMe { get; set; }
}

