using App.BLL.DTO.Vendors;
using App.DAL.DTO.Vendors;
using Base.Contracts;

namespace App.BLL.Mappers.Vendors;

public class VendorBllDtoMapper : IBaseMapper<VendorBllDto, VendorDalDto>
{
    public VendorBllDto? Map(VendorDalDto? entity)
    {
        if (entity is null) return null;

        return new VendorBllDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            RegistryCode = entity.RegistryCode,
            Notes = entity.Notes
        };
    }

    public VendorDalDto? Map(VendorBllDto? entity)
    {
        if (entity is null) return null;

        return new VendorDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            RegistryCode = entity.RegistryCode,
            Notes = entity.Notes
        };
    }
}

