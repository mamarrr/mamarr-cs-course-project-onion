using App.DAL.DTO.Identity;
using App.Domain.Identity;
using Base.Contracts;

namespace App.DAL.EF.Mappers;

public class AppRefreshTokenDalMapper : IBaseMapper<AppRefreshTokenDalDto, AppRefreshToken>
{
    public AppRefreshTokenDalDto? Map(AppRefreshToken? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new AppRefreshTokenDalDto
        {
            Id = entity.Id,
            RefreshToken = entity.RefreshToken,
            ExpirationDT = entity.ExpirationDT,
            PreviousRefreshToken = entity.PreviousRefreshToken,
            PreviousExpirationDT = entity.PreviousExpirationDT,
            AppUserId = entity.AppUserId
        };
    }

    public AppRefreshToken? Map(AppRefreshTokenDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new AppRefreshToken
        {
            Id = entity.Id,
            RefreshToken = entity.RefreshToken,
            ExpirationDT = entity.ExpirationDT,
            PreviousRefreshToken = entity.PreviousRefreshToken,
            PreviousExpirationDT = entity.PreviousExpirationDT,
            AppUserId = entity.AppUserId
        };
    }
}
