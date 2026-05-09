using App.DAL.Contracts.Repositories.Identity;
using App.DAL.DTO.Identity;
using App.DAL.EF.Mappers;
using App.Domain.Identity;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories.Identity;

public class AppRefreshTokenRepository :
    BaseRepository<AppRefreshTokenDalDto, AppRefreshToken, AppDbContext>,
    IAppRefreshTokenRepository
{
    private readonly AppDbContext _dbContext;

    public AppRefreshTokenRepository(AppDbContext dbContext, AppRefreshTokenDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<AppRefreshTokenDalDto?> FindByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.RefreshToken == refreshTokenHash, cancellationToken);

        return Mapper.Map(token);
    }

    public async Task<AppRefreshTokenDalDto?> FindByPreviousTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.PreviousRefreshToken == refreshTokenHash, cancellationToken);

        return Mapper.Map(token);
    }

    public async Task<AppRefreshTokenDalDto?> RotateAsync(
        Guid refreshTokenId,
        string newRefreshTokenHash,
        DateTime newExpiration,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(entity => entity.Id == refreshTokenId, cancellationToken);

        if (token is null)
        {
            return null;
        }

        token.PreviousRefreshToken = token.RefreshToken;
        token.PreviousExpirationDT = token.ExpirationDT;
        token.RefreshToken = newRefreshTokenHash;
        token.ExpirationDT = newExpiration;

        return Mapper.Map(token);
    }

    public async Task<bool> RemoveByIdAsync(
        Guid refreshTokenId,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(entity => entity.Id == refreshTokenId, cancellationToken);

        if (token is null)
        {
            return false;
        }

        _dbContext.RefreshTokens.Remove(token);
        return true;
    }

    public async Task<bool> RemoveByTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(entity => entity.RefreshToken == refreshTokenHash, cancellationToken);

        if (token is null)
        {
            return false;
        }

        _dbContext.RefreshTokens.Remove(token);
        return true;
    }
}
