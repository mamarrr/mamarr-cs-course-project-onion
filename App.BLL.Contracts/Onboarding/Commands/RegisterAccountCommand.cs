namespace App.BLL.Contracts.Onboarding.Commands;

public sealed class RegisterAccountCommand
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
}
