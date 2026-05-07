using App.DAL.DTO.Residents;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Residents;

public class ResidentContactDalMapper : IBaseMapper<ResidentContactDalDto, ResidentContact>
{
    public ResidentContactDalDto? Map(ResidentContact? entity)
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

    public ResidentContact? Map(ResidentContactDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ResidentContact
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
