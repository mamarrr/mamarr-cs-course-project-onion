using App.BLL.DTO.Residents;
using App.DTO.v1.Portal.Residents;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Residents;

public class ResidentApiMapper :
    IBaseMapper<ResidentRequestDto, ResidentBllDto>
{
    public ResidentRequestDto? Map(ResidentBllDto? entity)
    {
        return entity is null
            ? null
            : new ResidentRequestDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage
            };
    }

    public ResidentBllDto? Map(ResidentRequestDto? entity)
    {
        return entity is null
            ? null
            : new ResidentBllDto
            {
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                IdCode = entity.IdCode,
                PreferredLanguage = entity.PreferredLanguage
            };
    }
}
