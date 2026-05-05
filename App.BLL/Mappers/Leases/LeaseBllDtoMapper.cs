using App.BLL.Contracts.Leases;
using App.DAL.DTO.Leases;
using Base.Contracts;

namespace App.BLL.Mappers.Leases;

public class LeaseBllDtoMapper : IBaseMapper<LeaseBllDto, LeaseDalDto>
{
    public LeaseBllDto? Map(LeaseDalDto? entity)
    {
        if (entity is null) return null;

        return new LeaseBllDto
        {
            Id = entity.Id,
            UnitId = entity.UnitId,
            ResidentId = entity.ResidentId,
            LeaseRoleId = entity.LeaseRoleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Notes = entity.Notes
        };
    }

    public LeaseDalDto? Map(LeaseBllDto? entity)
    {
        if (entity is null) return null;

        return new LeaseDalDto
        {
            Id = entity.Id,
            UnitId = entity.UnitId,
            ResidentId = entity.ResidentId,
            LeaseRoleId = entity.LeaseRoleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Notes = entity.Notes
        };
    }
}

