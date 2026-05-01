using App.Contracts.DAL.Residents;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Residents;

public class ResidentDalMapper : IMapper<ResidentDalDto, Resident>
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
            IsActive = entity.IsActive
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
            IsActive = entity.IsActive
        };
    }
}
