using App.BLL.Contracts.Units;
using App.DAL.DTO.Units;
using Base.Contracts;

namespace App.BLL.Mappers.Units;

public class UnitBllDtoMapper : IBaseMapper<UnitBllDto, UnitDalDto>
{
    public UnitBllDto? Map(UnitDalDto? entity)
    {
        if (entity is null) return null;

        return new UnitBllDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            FloorNr = entity.FloorNr,
            SizeM2 = entity.SizeM2,
            Notes = entity.Notes
        };
    }

    public UnitDalDto? Map(UnitBllDto? entity)
    {
        if (entity is null) return null;

        return new UnitDalDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            FloorNr = entity.FloorNr,
            SizeM2 = entity.SizeM2,
            Notes = entity.Notes
        };
    }
}

