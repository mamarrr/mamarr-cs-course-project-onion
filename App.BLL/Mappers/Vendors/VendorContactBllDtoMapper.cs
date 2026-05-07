using App.BLL.DTO.Vendors;
using App.DAL.DTO.Vendors;
using Base.Contracts;

namespace App.BLL.Mappers.Vendors;

public class VendorContactBllDtoMapper : IBaseMapper<VendorContactBllDto, VendorContactDalDto>
{
    public VendorContactBllDto? Map(VendorContactDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorContactBllDto
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            ContactId = entity.ContactId,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Confirmed = entity.Confirmed,
            IsPrimary = entity.IsPrimary,
            FullName = entity.FullName,
            RoleTitle = entity.RoleTitle
        };
    }

    public VendorContactDalDto? Map(VendorContactBllDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorContactDalDto
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            ContactId = entity.ContactId,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Confirmed = entity.Confirmed,
            IsPrimary = entity.IsPrimary,
            FullName = entity.FullName,
            RoleTitle = entity.RoleTitle
        };
    }
}

