using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Tickets;
using App.DAL.EF.Mappers.Tickets;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class TicketRepository :
    BaseRepository<TicketDalDto, Ticket, AppDbContext>,
    ITicketRepository
{
    private readonly AppDbContext _dbContext;

    public TicketRepository(AppDbContext dbContext, TicketDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TicketListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        TicketListFilterDalDto filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ManagementCompanyId == managementCompanyId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(ticket =>
                ticket.TicketNr.ToLower().Contains(search) ||
                ticket.Customer!.Name.ToLower().Contains(search) ||
                ticket.Property!.AddressLine.ToLower().Contains(search) ||
                ticket.Unit!.UnitNr.ToLower().Contains(search) ||
                ticket.Resident!.FirstName.ToLower().Contains(search) ||
                ticket.Resident!.LastName.ToLower().Contains(search) ||
                ticket.Vendor!.Name.ToLower().Contains(search));
        }

        if (filter.StatusId.HasValue)
        {
            query = query.Where(ticket => ticket.TicketStatusId == filter.StatusId.Value);
        }

        if (filter.PriorityId.HasValue)
        {
            query = query.Where(ticket => ticket.TicketPriorityId == filter.PriorityId.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(ticket => ticket.TicketCategoryId == filter.CategoryId.Value);
        }

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(ticket => ticket.CustomerId == filter.CustomerId.Value);
        }

        if (filter.PropertyId.HasValue)
        {
            query = query.Where(ticket => ticket.PropertyId == filter.PropertyId.Value);
        }

        if (filter.UnitId.HasValue)
        {
            query = query.Where(ticket => ticket.UnitId == filter.UnitId.Value);
        }

        if (filter.ResidentId.HasValue)
        {
            query = query.Where(ticket => ticket.ResidentId == filter.ResidentId.Value);
        }

        if (filter.VendorId.HasValue)
        {
            query = query.Where(ticket => ticket.VendorId == filter.VendorId.Value);
        }

        if (filter.DueFrom.HasValue)
        {
            query = query.Where(ticket => ticket.DueAt.HasValue && ticket.DueAt.Value >= filter.DueFrom.Value);
        }

        if (filter.DueTo.HasValue)
        {
            query = query.Where(ticket => ticket.DueAt.HasValue && ticket.DueAt.Value <= filter.DueTo.Value);
        }

        return await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => new TicketListItemDalDto
            {
                Id = ticket.Id,
                TicketNr = ticket.TicketNr,
                Title = ticket.Title.ToString(),
                StatusCode = ticket.TicketStatus!.Code,
                StatusLabel = ticket.TicketStatus.Label.ToString(),
                PriorityLabel = ticket.TicketPriority!.Label.ToString(),
                CategoryLabel = ticket.TicketCategory!.Label.ToString(),
                CustomerName = ticket.Customer == null ? null : ticket.Customer.Name,
                CustomerSlug = ticket.Customer != null
                    ? ticket.Customer.Slug
                    : ticket.Property != null
                        ? ticket.Property.Customer!.Slug
                        : ticket.Unit != null
                            ? ticket.Unit.Property!.Customer!.Slug
                            : null,
                PropertyName = ticket.Property == null ? null : ticket.Property.Label.ToString(),
                PropertySlug = ticket.Property != null
                    ? ticket.Property.Slug
                    : ticket.Unit != null
                        ? ticket.Unit.Property!.Slug
                        : null,
                UnitNr = ticket.Unit == null ? null : ticket.Unit.UnitNr,
                UnitSlug = ticket.Unit == null ? null : ticket.Unit.Slug,
                ResidentName = ticket.Resident == null ? null : ticket.Resident.FirstName + " " + ticket.Resident.LastName,
                ResidentIdCode = ticket.Resident == null ? null : ticket.Resident.IdCode,
                VendorName = ticket.Vendor == null ? null : ticket.Vendor.Name,
                DueAt = ticket.DueAt,
                CreatedAt = ticket.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketDetailsDalDto?> FindDetailsAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId && ticket.ManagementCompanyId == managementCompanyId)
            .Select(ticket => new TicketDetailsDalDto
            {
                Id = ticket.Id,
                ManagementCompanyId = ticket.ManagementCompanyId,
                TicketNr = ticket.TicketNr,
                Title = ticket.Title.ToString(),
                Description = ticket.Description.ToString(),
                TicketStatusId = ticket.TicketStatusId,
                StatusCode = ticket.TicketStatus!.Code,
                StatusLabel = ticket.TicketStatus.Label.ToString(),
                TicketPriorityId = ticket.TicketPriorityId,
                PriorityLabel = ticket.TicketPriority!.Label.ToString(),
                TicketCategoryId = ticket.TicketCategoryId,
                CategoryLabel = ticket.TicketCategory!.Label.ToString(),
                CustomerId = ticket.CustomerId,
                CustomerName = ticket.Customer == null ? null : ticket.Customer.Name,
                CustomerSlug = ticket.Customer != null
                    ? ticket.Customer.Slug
                    : ticket.Property != null
                        ? ticket.Property.Customer!.Slug
                        : ticket.Unit != null
                            ? ticket.Unit.Property!.Customer!.Slug
                            : null,
                PropertyId = ticket.PropertyId,
                PropertyName = ticket.Property == null ? null : ticket.Property.Label.ToString(),
                PropertySlug = ticket.Property != null
                    ? ticket.Property.Slug
                    : ticket.Unit != null
                        ? ticket.Unit.Property!.Slug
                        : null,
                UnitId = ticket.UnitId,
                UnitNr = ticket.Unit == null ? null : ticket.Unit.UnitNr,
                UnitSlug = ticket.Unit == null ? null : ticket.Unit.Slug,
                ResidentId = ticket.ResidentId,
                ResidentName = ticket.Resident == null ? null : ticket.Resident.FirstName + " " + ticket.Resident.LastName,
                ResidentIdCode = ticket.Resident == null ? null : ticket.Resident.IdCode,
                VendorId = ticket.VendorId,
                VendorName = ticket.Vendor == null ? null : ticket.Vendor.Name,
                CreatedAt = ticket.CreatedAt,
                DueAt = ticket.DueAt,
                ClosedAt = ticket.ClosedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TicketEditDalDto?> FindForEditAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId && ticket.ManagementCompanyId == managementCompanyId)
            .Select(ticket => new TicketEditDalDto
            {
                Id = ticket.Id,
                ManagementCompanyId = ticket.ManagementCompanyId,
                TicketNr = ticket.TicketNr,
                Title = ticket.Title.ToString(),
                Description = ticket.Description.ToString(),
                TicketStatusId = ticket.TicketStatusId,
                StatusCode = ticket.TicketStatus!.Code,
                TicketPriorityId = ticket.TicketPriorityId,
                TicketCategoryId = ticket.TicketCategoryId,
                CustomerId = ticket.CustomerId,
                PropertyId = ticket.PropertyId,
                UnitId = ticket.UnitId,
                ResidentId = ticket.ResidentId,
                VendorId = ticket.VendorId,
                DueAt = ticket.DueAt,
                ClosedAt = ticket.ClosedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string> GetNextTicketNrAsync(
        Guid managementCompanyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var prefix = $"T-{utcNow.Year}-";
        var existing = await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ManagementCompanyId == managementCompanyId)
            .Where(ticket => ticket.TicketNr.StartsWith(prefix))
            .Select(ticket => ticket.TicketNr)
            .ToListAsync(cancellationToken);

        var max = existing
            .Select(ticketNr => int.TryParse(ticketNr[prefix.Length..], out var number) ? number : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{max + 1:0000}";
    }

    public async Task<bool> TicketNrExistsAsync(
        Guid managementCompanyId,
        string ticketNr,
        Guid? exceptTicketId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = ticketNr.Trim().ToLower();
        return await _dbContext.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.ManagementCompanyId == managementCompanyId)
            .Where(ticket => exceptTicketId == null || ticket.Id != exceptTicketId.Value)
            .AnyAsync(ticket => ticket.TicketNr.ToLower() == normalized, cancellationToken);
    }

    public override async Task<TicketDalDto> UpdateAsync(
        TicketDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var managementCompanyId = parentId == default ? dto.ManagementCompanyId : parentId;

        var ticket = await _dbContext.Tickets
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (ticket is null)
        {
            throw new ApplicationException($"Ticket with id {dto.Id} was not found.");
        }

        ticket.TicketNr = dto.TicketNr;
        ticket.TicketCategoryId = dto.TicketCategoryId;
        ticket.TicketStatusId = dto.TicketStatusId;
        ticket.TicketPriorityId = dto.TicketPriorityId;
        ticket.CustomerId = dto.CustomerId;
        ticket.PropertyId = dto.PropertyId;
        ticket.UnitId = dto.UnitId;
        ticket.ResidentId = dto.ResidentId;
        ticket.VendorId = dto.VendorId;
        ticket.DueAt = dto.DueAt;
        ticket.ClosedAt = dto.ClosedAt;
        
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            ticket.Title = null!;
            _dbContext.Entry(ticket).Property(entity => entity.Title).IsModified = true;
        }
        else if (ticket.Title is null)
        {
            ticket.Title = new LangStr(dto.Title.Trim());
            _dbContext.Entry(ticket).Property(entity => entity.Title).IsModified = true;
        }
        else
        {
            ticket.Title.SetTranslation(dto.Title.Trim());
            _dbContext.Entry(ticket).Property(entity => entity.Title).IsModified = true;
        }
        
        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            ticket.Description = null!;
            _dbContext.Entry(ticket).Property(entity => entity.Description).IsModified = true;
        }
        else if (ticket.Description is null)
        {
            ticket.Description = new LangStr(dto.Description.Trim());
            _dbContext.Entry(ticket).Property(entity => entity.Description).IsModified = true;
        }
        else
        {
            ticket.Description.SetTranslation(dto.Description.Trim());
            _dbContext.Entry(ticket).Property(entity => entity.Description).IsModified = true;
        }

        return Mapper.Map(ticket)!;
    }

    public async Task<bool> UpdateStatusAsync(
        TicketStatusUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.ManagementCompanyId == dto.ManagementCompanyId,
                cancellationToken);

        if (ticket is null)
        {
            return false;
        }

        ticket.TicketStatusId = dto.TicketStatusId;
        ticket.ClosedAt = dto.ClosedAt;
        return true;
    }

    public async Task<bool> HasDeleteDependenciesAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var scheduledWorkExists = await _dbContext.ScheduledWorks
            .AsNoTracking()
            .AnyAsync(
                work => work.TicketId == ticketId
                        && work.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (scheduledWorkExists)
        {
            return true;
        }

        return await _dbContext.WorkLogs
            .AsNoTracking()
            .AnyAsync(
                log => log.ScheduledWork!.TicketId == ticketId
                       && log.ScheduledWork.Ticket!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }
}
