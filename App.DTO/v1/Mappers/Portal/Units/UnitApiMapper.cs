using App.BLL.DTO.Units;
using App.DTO.v1.Portal.Units;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Units;

public class UnitApiMapper :
    IBaseMapper<UnitRequestDto, UnitBllDto>
{
    public UnitRequestDto? Map(UnitBllDto? entity)
    {
        return entity is null
            ? null
            : new UnitRequestDto
            {
                UnitNr = entity.UnitNr,
                FloorNr = entity.FloorNr,
                SizeM2 = entity.SizeM2,
                Notes = entity.Notes
            };
    }

    public UnitBllDto? Map(UnitRequestDto? entity)
    {
        return entity is null
            ? null
            : new UnitBllDto
            {
                UnitNr = entity.UnitNr,
                FloorNr = entity.FloorNr,
                SizeM2 = entity.SizeM2,
                Notes = entity.Notes
            };
    }
}
