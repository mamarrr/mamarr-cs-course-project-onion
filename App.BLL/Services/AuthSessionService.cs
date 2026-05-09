using System.Security.Cryptography;
using System.Text;
using App.BLL.Contracts.Auth;
using App.BLL.DTO.Auth;
using App.BLL.DTO.Common.Errors;
using App.DAL.Contracts;
using App.DAL.DTO.Identity;
using FluentResults;

namespace App.BLL.Services;

public class AuthSessionService : IAuthSessionService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private readonly IAppUOW _uow;

    public AuthSessionService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<Result<AuthSessionModel>> CreateSessionAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default)
    {
        if (appUserId == Guid.Empty)
        {
            return Result.Fail<AuthSessionModel>(new UnauthorizedError("Authenticated user is required."));
        }

        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);

        _uow.RefreshTokens.Add(new AppRefreshTokenDalDto
        {
            AppUserId = appUserId,
            RefreshToken = HashToken(refreshToken),
            ExpirationDT = expiresAt,
            PreviousExpirationDT = expiresAt
        });

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(new AuthSessionModel
        {
            AppUserId = appUserId,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        });
    }

    public async Task<Result<AuthSessionModel>> RotateSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Fail<AuthSessionModel>(new UnauthorizedError("Refresh token is required."));
        }

        var tokenHash = HashToken(refreshToken);
        var session = await _uow.RefreshTokens.FindByTokenHashAsync(tokenHash, cancellationToken);
        if (session is null)
        {
            var reusedSession = await _uow.RefreshTokens.FindByPreviousTokenHashAsync(tokenHash, cancellationToken);
            if (reusedSession is not null)
            {
                await _uow.RefreshTokens.RemoveByIdAsync(reusedSession.Id, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return Result.Fail<AuthSessionModel>(new UnauthorizedError("Refresh token is invalid."));
        }

        if (session.ExpirationDT <= DateTime.UtcNow)
        {
            await _uow.RefreshTokens.RemoveByIdAsync(session.Id, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return Result.Fail<AuthSessionModel>(new UnauthorizedError("Refresh token is expired."));
        }

        var newRefreshToken = GenerateRefreshToken();
        var newExpiration = DateTime.UtcNow.Add(RefreshTokenLifetime);
        var rotated = await _uow.RefreshTokens.RotateAsync(
            session.Id,
            HashToken(newRefreshToken),
            newExpiration,
            cancellationToken);

        if (rotated is null)
        {
            return Result.Fail<AuthSessionModel>(new UnauthorizedError("Refresh token is invalid."));
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return Result.Ok(new AuthSessionModel
        {
            AppUserId = rotated.AppUserId,
            RefreshToken = newRefreshToken,
            ExpiresAt = rotated.ExpirationDT
        });
    }

    public async Task<Result> RevokeSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Fail(new UnauthorizedError("Refresh token is required."));
        }

        var tokenHash = HashToken(refreshToken);
        var removed = await _uow.RefreshTokens.RemoveByTokenHashAsync(tokenHash, cancellationToken);

        if (!removed)
        {
            var reusedSession = await _uow.RefreshTokens.FindByPreviousTokenHashAsync(tokenHash, cancellationToken);
            if (reusedSession is not null)
            {
                removed = await _uow.RefreshTokens.RemoveByIdAsync(reusedSession.Id, cancellationToken);
            }
        }

        if (removed)
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return Result.Ok();
    }

    private static string GenerateRefreshToken()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string refreshToken)
    {
        return Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
