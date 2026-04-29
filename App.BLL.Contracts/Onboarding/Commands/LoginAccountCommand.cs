namespace App.BLL.Contracts.Onboarding.Commands;

public sealed class LoginAccountCommand
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public bool RememberMe { get; init; }
}
