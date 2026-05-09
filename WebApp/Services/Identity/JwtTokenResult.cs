namespace WebApp.Services.Identity;

public class JwtTokenResult
{
    public string Token { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
    public IdentityUserInfo User { get; init; } = default!;
}
