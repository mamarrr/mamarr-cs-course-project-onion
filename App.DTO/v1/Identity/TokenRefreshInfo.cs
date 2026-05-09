namespace App.DTO.v1.Identity;

public class TokenRefreshInfo
{
    public string? Jwt { get; set; }
    public string RefreshToken { get; set; } = default!;
}
