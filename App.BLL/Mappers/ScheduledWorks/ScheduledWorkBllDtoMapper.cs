using App.BLL.DTO.ScheduledWorks;
using App.DAL.DTO.ScheduledWorks;
using Base.Contracts;

namespace App.BLL.Mappers.ScheduledWorks;

public class ScheduledWorkBllDtoMapper : IBaseMapper<ScheduledWorkBllDto, ScheduledWorkDalDto>
{
    public ScheduledWorkBllDto? Map(ScheduledWorkDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new ScheduledWorkBllDto
        {
            Id = entity.Id,
            VendorId = entity.VendorId,
            TicketId = entity.TicketId,
            WorkStatusId = entity.WorkStatusId,
            ScheduledStart = entity.ScheduledStart,
            ScheduledEnd = entity.ScheduledEnd,
            RealStart = entity.RealStart,
            RealEnd = entity.RealEnd,
            Notes = entity.Notes
        };
    }

    public ScheduledWorkDalDto? Map(ScheduledWorkBllDto? entity)
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
            Notes = entity.Notes
        };
    }
}
