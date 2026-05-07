using App.DAL.Contracts.Repositories;
using App.DAL.DTO.WorkLogs;
using App.DAL.EF.Mappers.WorkLogs;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class WorkLogRepository :
    BaseRepository<WorkLogDalDto, WorkLog, AppDbContext>,
    IWorkLogRepository
{
    private readonly AppDbContext _dbContext;

    public WorkLogRepository(AppDbContext dbContext, WorkLogDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public override async Task<IEnumerable<WorkLogDalDto>> AllAsync(
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkLogs.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(log => log.ScheduledWork!.Ticket!.ManagementCompanyId == parentId);
        }

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(entity => Mapper.Map(entity)!);
    }

    public override async Task<WorkLogDalDto?> FindAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkLogs.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(log => log.ScheduledWork!.Ticket!.ManagementCompanyId == parentId);
        }

        return Mapper.Map(await query.FirstOrDefaultAsync(log => log.Id == id, cancellationToken));
    }

    public async Task<IReadOnlyList<WorkLogListItemDalDto>> AllByScheduledWorkAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await ProjectListItems(_dbContext.WorkLogs
                .AsNoTracking()
                .Where(log => log.ScheduledWorkId == scheduledWorkId
                              && log.ScheduledWork!.Ticket!.ManagementCompanyId == managementCompanyId))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkLogDalDto?> FindInCompanyAsync(
        Guid workLogId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WorkLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                log => log.Id == workLogId
                       && log.ScheduledWork!.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return Mapper.Map(entity);
    }

    public Task<bool> ExistsInCompanyAsync(
        Guid workLogId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkLogs
            .AsNoTracking()
            .AnyAsync(
                log => log.Id == workLogId
                       && log.ScheduledWork!.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> ExistsForScheduledWorkAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkLogs
            .AsNoTracking()
            .AnyAsync(
                log => log.ScheduledWorkId == scheduledWorkId
                       && log.ScheduledWork!.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> ExistsForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkLogs
            .AsNoTracking()
            .AnyAsync(
                log => log.ScheduledWork!.TicketId == ticketId
                       && log.ScheduledWork.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<WorkLogTotalsDalDto> TotalsForScheduledWorkAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await TotalsAsync(
            _dbContext.WorkLogs
                .AsNoTracking()
                .Where(log => log.ScheduledWorkId == scheduledWorkId
                              && log.ScheduledWork!.Ticket!.ManagementCompanyId == managementCompanyId),
            cancellationToken);
    }

    public async Task<WorkLogTotalsDalDto> TotalsForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await TotalsAsync(
            _dbContext.WorkLogs
                .AsNoTracking()
                .Where(log => log.ScheduledWork!.TicketId == ticketId
                              && log.ScheduledWork.Ticket!.ManagementCompanyId == managementCompanyId),
            cancellationToken);
    }

    public override async Task<WorkLogDalDto> UpdateAsync(
        WorkLogDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WorkLogs
            .AsTracking()
            .FirstOrDefaultAsync(
                log => log.Id == dto.Id
                       && log.ScheduledWork!.Ticket!.ManagementCompanyId == parentId,
                cancellationToken);

        if (entity is null)
        {
            throw new ApplicationException($"Work log with id {dto.Id} was not found.");
        }

        entity.WorkStart = dto.WorkStart;
        entity.WorkEnd = dto.WorkEnd;
        entity.Hours = dto.Hours;
        entity.MaterialCost = dto.MaterialCost;
        entity.LaborCost = dto.LaborCost;

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            entity.Description = null;
            _dbContext.Entry(entity).Property(log => log.Description).IsModified = true;
        }
        else if (entity.Description is null)
        {
            entity.Description = new LangStr(dto.Description.Trim());
            _dbContext.Entry(entity).Property(log => log.Description).IsModified = true;
        }
        else
        {
            entity.Description.SetTranslation(dto.Description.Trim());
            _dbContext.Entry(entity).Property(log => log.Description).IsModified = true;
        }

        return Mapper.Map(entity)!;
    }

    public async Task<bool> DeleteInCompanyAsync(
        Guid workLogId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WorkLogs
            .AsTracking()
            .FirstOrDefaultAsync(
                log => log.Id == workLogId
                       && log.ScheduledWork!.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.WorkLogs.Remove(entity);
        return true;
    }

    private static IQueryable<WorkLogListItemDalDto> ProjectListItems(IQueryable<WorkLog> query)
    {
        return query
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => new WorkLogListItemDalDto
            {
                Id = log.Id,
                ScheduledWorkId = log.ScheduledWorkId,
                AppUserId = log.AppUserId,
                AppUserName = log.AppUser!.FirstName + " " + log.AppUser.LastName,
                WorkStart = log.WorkStart,
                WorkEnd = log.WorkEnd,
                Hours = log.Hours,
                MaterialCost = log.MaterialCost,
                LaborCost = log.LaborCost,
                Description = log.Description == null ? null : log.Description.ToString(),
                CreatedAt = log.CreatedAt
            });
    }

    private static async Task<WorkLogTotalsDalDto> TotalsAsync(
        IQueryable<WorkLog> query,
        CancellationToken cancellationToken)
    {
        return await query
            .GroupBy(_ => 1)
            .Select(group => new WorkLogTotalsDalDto
            {
                Count = group.Count(),
                Hours = group.Sum(log => log.Hours ?? 0m),
                MaterialCost = group.Sum(log => log.MaterialCost ?? 0m),
                LaborCost = group.Sum(log => log.LaborCost ?? 0m)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new WorkLogTotalsDalDto();
    }
}
