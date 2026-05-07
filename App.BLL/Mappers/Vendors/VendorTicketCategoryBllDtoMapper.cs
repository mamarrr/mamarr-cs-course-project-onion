using App.BLL.DTO.Vendors;
using App.DAL.DTO.Vendors;
using Base.Contracts;

namespace App.BLL.Mappers.Vendors;

public class VendorTicketCategoryBllDtoMapper : IBaseMapper<VendorTicketCategoryBllDto, VendorTicketCategoryDalDto>
{
    public VendorTicketCategoryBllDto? Map(VendorTicketCategoryDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorTicketCategoryBllDto
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            TicketCategoryId = entity.TicketCategoryId,
            Notes = entity.Notes
        };
    }

    public VendorTicketCategoryDalDto? Map(VendorTicketCategoryBllDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorTicketCategoryDalDto
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            TicketCategoryId = entity.TicketCategoryId,
            Notes = entity.Notes
        };
    }
}

