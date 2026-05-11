using App.BLL.DTO.WorkLogs;
using App.DTO.v1.Portal.WorkLogs;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.WorkLogs;

public class WorkLogApiMapper :
    IBaseMapper<WorkLogRequestDto, WorkLogBllDto>
{
    public WorkLogBllDto? Map(WorkLogRequestDto? entity)
    {
        return entity is null
            ? null
            : new WorkLogBllDto
            {
                WorkStart = entity.WorkStart,
                WorkEnd = entity.WorkEnd,
                Hours = entity.Hours,
                MaterialCost = entity.MaterialCost,
                LaborCost = entity.LaborCost,
                Description = entity.Description
            };
    }

    public WorkLogRequestDto? Map(WorkLogBllDto? entity)
    {
        return entity is null
            ? null
            : new WorkLogRequestDto
            {
                WorkStart = entity.WorkStart,
                WorkEnd = entity.WorkEnd,
                Hours = entity.Hours,
                MaterialCost = entity.MaterialCost,
                LaborCost = entity.LaborCost,
                Description = entity.Description
            };
    }
}
