using App.Contracts.DAL.Residents;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Residents;

public sealed class ResidentDalMapper : IMapper<ResidentDalDto, Resident>
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

    public ResidentProfileDalDto MapProfile(Resident entity)
    {
        return new ResidentProfileDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            CompanySlug = entity.ManagementCompany!.Slug,
            CompanyName = entity.ManagementCompany.Name,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            IdCode = entity.IdCode,
            PreferredLanguage = entity.PreferredLanguage,
            IsActive = entity.IsActive
        };
    }

    public ResidentListItemDalDto MapListItem(Resident entity)
    {
        return new ResidentListItemDalDto
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
