using App.BLL.DTO.Auth;
using FluentResults;

namespace App.BLL.Contracts.Auth;

public interface IAuthSessionService
{
    Task<Result<AuthSessionModel>> CreateSessionAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<Result<AuthSessionModel>> RotateSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
