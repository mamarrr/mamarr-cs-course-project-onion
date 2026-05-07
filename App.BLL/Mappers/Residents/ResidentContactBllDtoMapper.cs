using App.BLL.DTO.Residents;
using App.DAL.DTO.Residents;
using Base.Contracts;

namespace App.BLL.Mappers.Residents;

public class ResidentContactBllDtoMapper : IBaseMapper<ResidentContactBllDto, ResidentContactDalDto>
{
    public ResidentContactBllDto? Map(ResidentContactDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ResidentContactBllDto
        {
            Id = entity.Id,
            ResidentId = entity.ResidentId,
            ContactId = entity.ContactId,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Confirmed = entity.Confirmed,
            IsPrimary = entity.IsPrimary
        };
    }

    public ResidentContactDalDto? Map(ResidentContactBllDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ResidentContactDalDto
        {
            Id = entity.Id,
            ResidentId = entity.ResidentId,
            ContactId = entity.ContactId,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Confirmed = entity.Confirmed,
            IsPrimary = entity.IsPrimary
        };
    }
}
