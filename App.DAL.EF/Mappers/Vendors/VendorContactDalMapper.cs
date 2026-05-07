using App.DAL.DTO.Vendors;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Vendors;

public class VendorContactDalMapper : IBaseMapper<VendorContactDalDto, VendorContact>
{
    public VendorContactDalDto? Map(VendorContact? entity)
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
            RoleTitle = entity.RoleTitle?.ToString()
        };
    }

    public VendorContact? Map(VendorContactDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorContact
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            ContactId = entity.ContactId,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Confirmed = entity.Confirmed,
            IsPrimary = entity.IsPrimary,
            FullName = entity.FullName,
            RoleTitle = string.IsNullOrWhiteSpace(entity.RoleTitle) ? null : new LangStr(entity.RoleTitle.Trim())
        };
    }
}

