using App.BLL.Contracts.ManagementCompanies;
using App.DAL.DTO.ManagementCompanies;
using Base.Contracts;

namespace App.BLL.Mappers.ManagementCompanies;

public class ManagementCompanyBllDtoMapper : IBaseMapper<ManagementCompanyBllDto, ManagementCompanyDalDto>
{
    public ManagementCompanyBllDto? Map(ManagementCompanyDalDto? entity)
    {
        if (entity is null) return null;

        return new ManagementCompanyBllDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            VatNumber = entity.VatNumber,
            Email = entity.Email,
            Phone = entity.Phone,
            Address = entity.Address
        };
    }

    public ManagementCompanyDalDto? Map(ManagementCompanyBllDto? entity)
    {
        if (entity is null) return null;

        return new ManagementCompanyDalDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            VatNumber = entity.VatNumber,
            Email = entity.Email,
            Phone = entity.Phone,
            Address = entity.Address
        };
    }
}

public class ManagementCompanyJoinRequestBllDtoMapper :
    IBaseMapper<ManagementCompanyJoinRequestBllDto, ManagementCompanyJoinRequestDalDto>
{
    public ManagementCompanyJoinRequestBllDto? Map(ManagementCompanyJoinRequestDalDto? entity)
    {
        if (entity is null) return null;

        return new ManagementCompanyJoinRequestBllDto
        {
            Id = entity.Id,
            AppUserId = entity.AppUserId,
            ManagementCompanyId = entity.ManagementCompanyId,
            RequestedRoleId = entity.RequestedRoleId,
            StatusId = entity.StatusId,
            Message = entity.Message,
            ResolvedAt = entity.ResolvedAt,
            ResolvedByAppUserId = entity.ResolvedByAppUserId
        };
    }

    public ManagementCompanyJoinRequestDalDto? Map(ManagementCompanyJoinRequestBllDto? entity)
    {
        if (entity is null) return null;

        return new ManagementCompanyJoinRequestDalDto
        {
            Id = entity.Id,
            AppUserId = entity.AppUserId,
            ManagementCompanyId = entity.ManagementCompanyId,
            RequestedRoleId = entity.RequestedRoleId,
            StatusId = entity.StatusId,
            Message = entity.Message,
            ResolvedAt = entity.ResolvedAt,
            ResolvedByAppUserId = entity.ResolvedByAppUserId
        };
    }
}

