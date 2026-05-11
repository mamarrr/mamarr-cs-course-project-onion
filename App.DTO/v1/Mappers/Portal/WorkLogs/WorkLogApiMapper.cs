using App.BLL.DTO.WorkLogs;
using App.DTO.v1.Portal.WorkLogs;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.WorkLogs;

public sealed class WorkLogApiMapper :
    IBaseMapper<CreateWorkLogDto, WorkLogBllDto>,
    IBaseMapper<UpdateWorkLogDto, WorkLogBllDto>
{
    public WorkLogBllDto? Map(CreateWorkLogDto? entity)
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

    public WorkLogBllDto? Map(UpdateWorkLogDto? entity)
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

    CreateWorkLogDto? IBaseMapper<CreateWorkLogDto, WorkLogBllDto>.Map(WorkLogBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateWorkLogDto
            {
                WorkStart = entity.WorkStart,
                WorkEnd = entity.WorkEnd,
                Hours = entity.Hours,
                MaterialCost = entity.MaterialCost,
                LaborCost = entity.LaborCost,
                Description = entity.Description
            };
    }

    UpdateWorkLogDto? IBaseMapper<UpdateWorkLogDto, WorkLogBllDto>.Map(WorkLogBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateWorkLogDto
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
