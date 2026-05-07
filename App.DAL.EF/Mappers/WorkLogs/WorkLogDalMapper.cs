using App.DAL.DTO.WorkLogs;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.WorkLogs;

public class WorkLogDalMapper : IBaseMapper<WorkLogDalDto, WorkLog>
{
    public WorkLogDalDto? Map(WorkLog? entity)
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
            Description = entity.Description?.ToString()
        };
    }

    public WorkLog? Map(WorkLogDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new WorkLog
        {
            Id = entity.Id,
            ScheduledWorkId = entity.ScheduledWorkId,
            AppUserId = entity.AppUserId,
            WorkStart = entity.WorkStart,
            WorkEnd = entity.WorkEnd,
            Hours = entity.Hours,
            MaterialCost = entity.MaterialCost,
            LaborCost = entity.LaborCost,
            Description = string.IsNullOrWhiteSpace(entity.Description)
                ? null
                : new LangStr(entity.Description.Trim())
        };
    }
}
