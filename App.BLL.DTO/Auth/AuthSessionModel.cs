namespace App.BLL.DTO.Auth;

public class AuthSessionModel
{
    public Guid AppUserId { get; init; }
    public string RefreshToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}
