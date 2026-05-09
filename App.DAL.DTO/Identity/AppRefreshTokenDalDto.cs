using Base.Domain;

namespace App.DAL.DTO.Identity;

public class AppRefreshTokenDalDto : BaseEntity
{
    public string RefreshToken { get; set; } = default!;
    public DateTime ExpirationDT { get; set; }
    public string? PreviousRefreshToken { get; set; }
    public DateTime PreviousExpirationDT { get; set; }
    public Guid AppUserId { get; set; }
}
