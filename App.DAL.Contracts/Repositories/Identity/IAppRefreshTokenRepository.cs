using App.DAL.DTO.Identity;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories.Identity;

public interface IAppRefreshTokenRepository : IBaseRepository<AppRefreshTokenDalDto>
{
    Task<AppRefreshTokenDalDto?> FindByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<AppRefreshTokenDalDto?> FindByPreviousTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);

    Task<AppRefreshTokenDalDto?> RotateAsync(
        Guid refreshTokenId,
        string newRefreshTokenHash,
        DateTime newExpiration,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveByIdAsync(
        Guid refreshTokenId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default);
}
