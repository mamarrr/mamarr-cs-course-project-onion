namespace App.BLL.DTO.Onboarding.Commands;

public class LoginAccountCommand
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public bool RememberMe { get; init; }
}
