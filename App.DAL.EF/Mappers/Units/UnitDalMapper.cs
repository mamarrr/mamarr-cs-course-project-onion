using App.DAL.DTO.Units;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Units;

public class UnitDalMapper : IBaseMapper<UnitDalDto, Unit>
{
    public UnitDalDto? Map(Unit? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new UnitDalDto
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            FloorNr = entity.FloorNr,
            SizeM2 = entity.SizeM2,
            Notes = entity.Notes?.ToString(),
            CreatedAt = entity.CreatedAt
        };
    }

    public Unit? Map(UnitDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Unit
        {
            Id = entity.Id,
            PropertyId = entity.PropertyId,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            FloorNr = entity.FloorNr,
            SizeM2 = entity.SizeM2,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim()),
            CreatedAt = entity.CreatedAt
        };
    }
}
