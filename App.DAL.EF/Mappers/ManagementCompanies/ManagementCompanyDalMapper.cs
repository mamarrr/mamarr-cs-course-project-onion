using App.Contracts.DAL.ManagementCompanies;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.ManagementCompanies;

public sealed class ManagementCompanyDalMapper : IBaseMapper<ManagementCompanyDalDto, ManagementCompany>
{
    public ManagementCompanyDalDto? Map(ManagementCompany? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ManagementCompanyDalDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }

    public ManagementCompany? Map(ManagementCompanyDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ManagementCompany
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }
}
