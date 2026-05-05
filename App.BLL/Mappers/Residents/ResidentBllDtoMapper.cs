using App.BLL.Contracts.Residents;
using App.DAL.DTO.Residents;
using Base.Contracts;

namespace App.BLL.Mappers.Residents;

public class ResidentBllDtoMapper : IBaseMapper<ResidentBllDto, ResidentDalDto>
{
    public ResidentBllDto? Map(ResidentDalDto? entity)
    {
        if (entity is null) return null;

        return new ResidentBllDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            IdCode = entity.IdCode,
            PreferredLanguage = entity.PreferredLanguage
        };
    }

    public ResidentDalDto? Map(ResidentBllDto? entity)
    {
        if (entity is null) return null;

        return new ResidentDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            IdCode = entity.IdCode,
            PreferredLanguage = entity.PreferredLanguage
        };
    }
}

