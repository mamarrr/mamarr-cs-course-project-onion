using App.DAL.Contracts.Repositories;
using App.DAL.DTO.ScheduledWorks;
using App.DAL.EF.Mappers.ScheduledWorks;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class ScheduledWorkRepository :
    BaseRepository<ScheduledWorkDalDto, ScheduledWork, AppDbContext>,
    IScheduledWorkRepository
{
    private readonly AppDbContext _dbContext;

    public ScheduledWorkRepository(AppDbContext dbContext, ScheduledWorkDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public override async Task<IEnumerable<ScheduledWorkDalDto>> AllAsync(
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ScheduledWorks.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(work => work.Ticket!.ManagementCompanyId == parentId);
        }

        var entities = await query.ToListAsync(cancellationToken);
        return entities.Select(entity => Mapper.Map(entity)!);
    }

    public override async Task<ScheduledWorkDalDto?> FindAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ScheduledWorks.AsNoTracking();
        if (parentId != default)
        {
            query = query.Where(work => work.Ticket!.ManagementCompanyId == parentId);
        }

        var entity = await query.FirstOrDefaultAsync(work => work.Id == id, cancellationToken);
        return Mapper.Map(entity);
    }

    public async Task<IReadOnlyList<ScheduledWorkListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await ProjectListItems(_dbContext.ScheduledWorks
                .AsNoTracking()
                .Where(work => work.Ticket!.ManagementCompanyId == managementCompanyId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledWorkListItemDalDto>> AllByTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await ProjectListItems(_dbContext.ScheduledWorks
                .AsNoTracking()
                .Where(work => work.TicketId == ticketId
                               && work.Ticket!.ManagementCompanyId == managementCompanyId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ScheduledWorkDetailsDalDto?> FindDetailsAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ScheduledWorks
            .AsNoTracking()
            .Where(work => work.Id == scheduledWorkId
                           && work.Ticket!.ManagementCompanyId == managementCompanyId)
            .Select(work => new ScheduledWorkDetailsDalDto
            {
                Id = work.Id,
                TicketId = work.TicketId,
                TicketNr = work.Ticket!.TicketNr,
                TicketTitle = work.Ticket.Title.ToString(),
                CompanySlug = work.Ticket.ManagementCompany!.Slug,
                CompanyName = work.Ticket.ManagementCompany.Name,
                VendorId = work.VendorId,
                VendorName = work.Vendor!.Name,
                WorkStatusId = work.WorkStatusId,
                WorkStatusCode = work.WorkStatus!.Code,
                WorkStatusLabel = work.WorkStatus.Label.ToString(),
                ScheduledStart = work.ScheduledStart,
                ScheduledEnd = work.ScheduledEnd,
                RealStart = work.RealStart,
                RealEnd = work.RealEnd,
                Notes = work.Notes == null ? null : work.Notes.ToString(),
                CreatedAt = work.CreatedAt,
                WorkLogCount = work.WorkLogs!.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ScheduledWorkDalDto?> FindInCompanyAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ScheduledWorks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                work => work.Id == scheduledWorkId
                        && work.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        return Mapper.Map(entity);
    }

    public Task<bool> ExistsForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ScheduledWorks
            .AsNoTracking()
            .AnyAsync(
                work => work.TicketId == ticketId
                        && work.Ticket!.ManagementCompanyId == managementCompanyId
                        && work.WorkStatus!.Code != "CANCELLED",
                cancellationToken);
    }

    public Task<bool> HasWorkLogsAsync(
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

    public Task<bool> VendorBelongsToTicketCompanyAsync(
        Guid vendorId,
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Vendors
            .AsNoTracking()
            .AnyAsync(
                vendor => vendor.Id == vendorId
                          && vendor.ManagementCompanyId == managementCompanyId
                          && _dbContext.Tickets.Any(ticket =>
                              ticket.Id == ticketId && ticket.ManagementCompanyId == managementCompanyId),
                cancellationToken);
    }

    public Task<bool> VendorSupportsTicketCategoryAsync(
        Guid vendorId,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId)
            .AnyAsync(
                ticket => _dbContext.VendorTicketCategories.Any(category =>
                    category.VendorId == vendorId
                    && category.TicketCategoryId == ticket.TicketCategoryId
                    && category.Vendor!.ManagementCompanyId == ticket.ManagementCompanyId),
                cancellationToken);
    }

    public Task<bool> AnyStartedForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ScheduledWorks
            .AsNoTracking()
            .AnyAsync(
                work => work.TicketId == ticketId
                        && work.Ticket!.ManagementCompanyId == managementCompanyId
                        && work.RealStart.HasValue
                        && work.WorkStatus!.Code != "CANCELLED",
                cancellationToken);
    }

    public Task<bool> AnyCompletedForTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ScheduledWorks
            .AsNoTracking()
            .AnyAsync(
                work => work.TicketId == ticketId
                        && work.Ticket!.ManagementCompanyId == managementCompanyId
                        && work.RealEnd.HasValue
                        && work.WorkStatus!.Code != "CANCELLED",
                cancellationToken);
    }

    public override async Task<ScheduledWorkDalDto> UpdateAsync(
        ScheduledWorkDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ScheduledWorks
            .AsTracking()
            .FirstOrDefaultAsync(
                work => work.Id == dto.Id
                        && work.Ticket!.ManagementCompanyId == parentId,
                cancellationToken);

        if (entity is null)
        {
            throw new ApplicationException($"Scheduled work with id {dto.Id} was not found.");
        }

        entity.VendorId = dto.VendorId;
        entity.WorkStatusId = dto.WorkStatusId;
        entity.ScheduledStart = dto.ScheduledStart;
        entity.ScheduledEnd = dto.ScheduledEnd;
        entity.RealStart = dto.RealStart;
        entity.RealEnd = dto.RealEnd;

        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            entity.Notes = null;
            _dbContext.Entry(entity).Property(work => work.Notes).IsModified = true;
        }
        else if (entity.Notes is null)
        {
            entity.Notes = new LangStr(dto.Notes.Trim());
            _dbContext.Entry(entity).Property(work => work.Notes).IsModified = true;
        }
        else
        {
            entity.Notes.SetTranslation(dto.Notes.Trim());
            _dbContext.Entry(entity).Property(work => work.Notes).IsModified = true;
        }

        return Mapper.Map(entity)!;
    }

    public async Task<bool> DeleteInCompanyAsync(
        Guid scheduledWorkId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ScheduledWorks
            .AsTracking()
            .FirstOrDefaultAsync(
                work => work.Id == scheduledWorkId
                        && work.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.ScheduledWorks.Remove(entity);
        return true;
    }

    private static IQueryable<ScheduledWorkListItemDalDto> ProjectListItems(IQueryable<ScheduledWork> query)
    {
        return query
            .OrderBy(work => work.ScheduledStart)
            .Select(work => new ScheduledWorkListItemDalDto
            {
                Id = work.Id,
                VendorId = work.VendorId,
                VendorName = work.Vendor!.Name,
                WorkStatusId = work.WorkStatusId,
                WorkStatusCode = work.WorkStatus!.Code,
                WorkStatusLabel = work.WorkStatus.Label.ToString(),
                ScheduledStart = work.ScheduledStart,
                ScheduledEnd = work.ScheduledEnd,
                RealStart = work.RealStart,
                RealEnd = work.RealEnd,
                Notes = work.Notes == null ? null : work.Notes.ToString(),
                CreatedAt = work.CreatedAt,
                WorkLogCount = work.WorkLogs!.Count
            });
    }
}
