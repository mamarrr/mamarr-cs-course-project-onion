using App.DAL.Contracts.DAL.Units;
using App.Domain;
using Base.Contracts;

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
            CustomerId = entity.Property?.CustomerId ?? Guid.Empty,
            ManagementCompanyId = entity.Property?.Customer?.ManagementCompanyId ?? Guid.Empty,
            UnitNr = entity.UnitNr,
            Slug = entity.Slug,
            IsActive = entity.IsActive
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
            IsActive = entity.IsActive
        };
    }
}
