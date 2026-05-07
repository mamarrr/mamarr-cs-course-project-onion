using App.DAL.DTO.ScheduledWorks;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.ScheduledWorks;

public class ScheduledWorkDalMapper : IBaseMapper<ScheduledWorkDalDto, ScheduledWork>
{
    public ScheduledWorkDalDto? Map(ScheduledWork? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ScheduledWorkDalDto
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            TicketId = entity.TicketId,
            WorkStatusId = entity.WorkStatusId,
            ScheduledStart = entity.ScheduledStart,
            ScheduledEnd = entity.ScheduledEnd,
            RealStart = entity.RealStart,
            RealEnd = entity.RealEnd,
            Notes = entity.Notes?.ToString()
        };
    }

    public ScheduledWork? Map(ScheduledWorkDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ScheduledWork
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            TicketId = entity.TicketId,
            WorkStatusId = entity.WorkStatusId,
            ScheduledStart = entity.ScheduledStart,
            ScheduledEnd = entity.ScheduledEnd,
            RealStart = entity.RealStart,
            RealEnd = entity.RealEnd,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim())
        };
    }
}
