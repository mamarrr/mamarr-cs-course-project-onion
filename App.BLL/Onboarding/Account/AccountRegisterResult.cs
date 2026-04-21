namespace App.BLL.Onboarding.Account;

public class AccountRegisterResult
{
    public bool Succeeded { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();
}

