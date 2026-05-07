using App.DAL.DTO.Vendors;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Vendors;

public class VendorTicketCategoryDalMapper : IBaseMapper<VendorTicketCategoryDalDto, VendorTicketCategory>
{
    public VendorTicketCategoryDalDto? Map(VendorTicketCategory? entity)
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
            Notes = entity.Notes?.ToString()
        };
    }

    public VendorTicketCategory? Map(VendorTicketCategoryDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new VendorTicketCategory
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            TicketCategoryId = entity.TicketCategoryId,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim())
        };
    }
}

