using App.BLL.DTO.Units;
using App.DTO.v1.Portal.Units;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Units;

public sealed class UnitApiMapper :
    IBaseMapper<CreateUnitDto, UnitBllDto>,
    IBaseMapper<UpdateUnitProfileDto, UnitBllDto>
{
    CreateUnitDto? IBaseMapper<CreateUnitDto, UnitBllDto>.Map(UnitBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateUnitDto
            {
                UnitNr = entity.UnitNr,
                FloorNr = entity.FloorNr,
                SizeM2 = entity.SizeM2,
                Notes = entity.Notes
            };
    }

    UnitBllDto? IBaseMapper<CreateUnitDto, UnitBllDto>.Map(CreateUnitDto? entity)
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

    UpdateUnitProfileDto? IBaseMapper<UpdateUnitProfileDto, UnitBllDto>.Map(UnitBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateUnitProfileDto
            {
                UnitNr = entity.UnitNr,
                FloorNr = entity.FloorNr,
                SizeM2 = entity.SizeM2,
                Notes = entity.Notes
            };
    }

    UnitBllDto? IBaseMapper<UpdateUnitProfileDto, UnitBllDto>.Map(UpdateUnitProfileDto? entity)
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
