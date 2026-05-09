using FluentResults;

namespace WebApp.Services.Identity;

public interface IJwtTokenService
{
    Task<Result<JwtTokenResult>> CreateAccessTokenAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);
}
