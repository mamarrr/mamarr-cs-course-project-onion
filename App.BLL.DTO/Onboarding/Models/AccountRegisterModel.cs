namespace App.BLL.DTO.Onboarding.Models;

public class AccountRegisterModel
{
    public Guid AppUserId { get; init; }
    public string Email { get; init; } = default!;
}
