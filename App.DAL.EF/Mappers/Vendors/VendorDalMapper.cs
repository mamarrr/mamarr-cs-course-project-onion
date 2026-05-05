using App.DAL.DTO.Vendors;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Vendors;

public class VendorDalMapper : IBaseMapper<VendorDalDto, Vendor>
{
    public VendorDalDto? Map(Vendor? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            RegistryCode = entity.RegistryCode,
            Notes = entity.Notes.ToString(),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public Vendor? Map(VendorDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Vendor
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            RegistryCode = entity.RegistryCode,
            Notes = new LangStr(entity.Notes),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }
}
