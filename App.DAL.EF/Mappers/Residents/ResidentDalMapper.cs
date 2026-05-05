using App.DAL.DTO.Residents;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Residents;

public class ResidentDalMapper : IBaseMapper<ResidentDalDto, Resident>
{
    public ResidentDalDto? Map(Resident? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ResidentDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            IdCode = entity.IdCode,
            PreferredLanguage = entity.PreferredLanguage,
            CreatedAt = entity.CreatedAt
        };
    }

    public Resident? Map(ResidentDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Resident
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            IdCode = entity.IdCode,
            PreferredLanguage = entity.PreferredLanguage,
            CreatedAt = entity.CreatedAt
        };
    }
}
