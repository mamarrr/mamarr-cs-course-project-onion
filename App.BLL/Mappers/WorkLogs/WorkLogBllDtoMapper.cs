using App.BLL.DTO.WorkLogs;
using App.DAL.DTO.WorkLogs;
using Base.Contracts;

namespace App.BLL.Mappers.WorkLogs;

public class WorkLogBllDtoMapper : IBaseMapper<WorkLogBllDto, WorkLogDalDto>
{
    public WorkLogBllDto? Map(WorkLogDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new WorkLogBllDto
        {
            Id = entity.Id,
            ScheduledWorkId = entity.ScheduledWorkId,
            AppUserId = entity.AppUserId,
            WorkStart = entity.WorkStart,
            WorkEnd = entity.WorkEnd,
            Hours = entity.Hours,
            MaterialCost = entity.MaterialCost,
            LaborCost = entity.LaborCost,
            Description = entity.Description
        };
    }

    public WorkLogDalDto? Map(WorkLogBllDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new WorkLogDalDto
        {
            Id = entity.Id,
            ScheduledWorkId = entity.ScheduledWorkId,
            AppUserId = entity.AppUserId,
            WorkStart = entity.WorkStart,
            WorkEnd = entity.WorkEnd,
            Hours = entity.Hours,
            MaterialCost = entity.MaterialCost,
            LaborCost = entity.LaborCost,
            Description = entity.Description
        };
    }
}
