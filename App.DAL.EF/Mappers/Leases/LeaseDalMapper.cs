using App.DAL.DTO.Leases;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Leases;

public class LeaseDalMapper : IBaseMapper<LeaseDalDto, Lease>
{
    public LeaseDalDto? Map(Lease? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new LeaseDalDto
        {
            Id = entity.Id,
            UnitId = entity.UnitId,
            ResidentId = entity.ResidentId,
            LeaseRoleId = entity.LeaseRoleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Notes = entity.Notes?.ToString()
        };
    }

    public Lease? Map(LeaseDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Lease
        {
            Id = entity.Id,
            UnitId = entity.UnitId,
            ResidentId = entity.ResidentId,
            LeaseRoleId = entity.LeaseRoleId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim())
        };
    }
}
