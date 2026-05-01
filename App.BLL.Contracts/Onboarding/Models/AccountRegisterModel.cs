namespace App.BLL.Contracts.Onboarding.Models;

public class AccountRegisterModel
{
    public Guid AppUserId { get; init; }
    public string Email { get; init; } = default!;
}
