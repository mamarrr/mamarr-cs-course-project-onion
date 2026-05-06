namespace App.BLL.DTO.Onboarding.Models;

public class AccountLoginModel
{
    public Guid AppUserId { get; init; }
    public string Email { get; init; } = default!;
}
