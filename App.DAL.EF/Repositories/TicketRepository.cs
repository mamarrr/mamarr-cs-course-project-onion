using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Tickets;
using App.Domain;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _dbContext;

    public TicketRepository(AppDbContext dbContext)
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
                Title = ticket.Title,
                Description = ticket.Description,
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

    public Task<TicketOptionDalDto?> FindStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        return _dbContext.TicketStatuses
            .AsNoTracking()
            .Where(status => status.Code == normalized)
            .Select(status => new TicketOptionDalDto
            {
                Id = status.Id,
                Code = status.Code,
                Label = status.Label.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TicketOptionDalDto?> FindStatusByIdAsync(
        Guid statusId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TicketStatuses
            .AsNoTracking()
            .Where(status => status.Id == statusId)
            .Select(status => new TicketOptionDalDto
            {
                Id = status.Id,
                Code = status.Code,
                Label = status.Label.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TicketOptionDalDto>> AllStatusesAsync(CancellationToken cancellationToken = default)
    {
        return LookupOptions(_dbContext.TicketStatuses).ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<TicketOptionDalDto>)task.Result, cancellationToken);
    }

    public Task<IReadOnlyList<TicketOptionDalDto>> AllPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        return LookupOptions(_dbContext.TicketPriorities).ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<TicketOptionDalDto>)task.Result, cancellationToken);
    }

    public Task<IReadOnlyList<TicketOptionDalDto>> AllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return LookupOptions(_dbContext.TicketCategories).ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<TicketOptionDalDto>)task.Result, cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> CustomerOptionsAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.ManagementCompanyId == managementCompanyId && customer.IsActive)
            .OrderBy(customer => customer.Name)
            .Select(customer => new TicketOptionDalDto
            {
                Id = customer.Id,
                Label = customer.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> PropertyOptionsAsync(
        Guid managementCompanyId,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.Customer!.ManagementCompanyId == managementCompanyId && property.IsActive);

        if (customerId.HasValue)
        {
            query = query.Where(property => property.CustomerId == customerId.Value);
        }

        return await query
            .OrderBy(property => property.Label)
            .Select(property => new TicketOptionDalDto
            {
                Id = property.Id,
                Label = property.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> UnitOptionsAsync(
        Guid managementCompanyId,
        Guid? propertyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.Property!.Customer!.ManagementCompanyId == managementCompanyId && unit.IsActive);

        if (propertyId.HasValue)
        {
            query = query.Where(unit => unit.PropertyId == propertyId.Value);
        }

        return await query
            .OrderBy(unit => unit.UnitNr)
            .Select(unit => new TicketOptionDalDto
            {
                Id = unit.Id,
                Label = unit.UnitNr
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> ResidentOptionsAsync(
        Guid managementCompanyId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Residents
            .AsNoTracking()
            .Where(resident => resident.ManagementCompanyId == managementCompanyId && resident.IsActive);

        if (unitId.HasValue)
        {
            query = query.Where(resident => resident.Leases!.Any(lease => lease.UnitId == unitId.Value && lease.IsActive));
        }

        return await query
            .OrderBy(resident => resident.LastName)
            .ThenBy(resident => resident.FirstName)
            .Select(resident => new TicketOptionDalDto
            {
                Id = resident.Id,
                Label = resident.FirstName + " " + resident.LastName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> VendorOptionsAsync(
        Guid managementCompanyId,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.ManagementCompanyId == managementCompanyId && vendor.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(vendor => vendor.VendorTicketCategories!
                .Any(link => link.TicketCategoryId == categoryId.Value && link.IsActive));
        }

        return await query
            .OrderBy(vendor => vendor.Name)
            .Select(vendor => new TicketOptionDalDto
            {
                Id = vendor.Id,
                Label = vendor.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TicketReferenceValidationDalDto> ValidateReferencesAsync(
        Guid managementCompanyId,
        Guid categoryId,
        Guid priorityId,
        Guid statusId,
        Guid? customerId,
        Guid? propertyId,
        Guid? unitId,
        Guid? residentId,
        Guid? vendorId,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _dbContext.TicketCategories
            .AsNoTracking()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);

        var priorityExists = await _dbContext.TicketPriorities
            .AsNoTracking()
            .AnyAsync(priority => priority.Id == priorityId, cancellationToken);

        var statusExists = await _dbContext.TicketStatuses
            .AsNoTracking()
            .AnyAsync(status => status.Id == statusId, cancellationToken);

        var customerBelongs = !customerId.HasValue || await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(customer => customer.Id == customerId.Value && customer.ManagementCompanyId == managementCompanyId, cancellationToken);

        var propertyBelongs = !propertyId.HasValue || await _dbContext.Properties
            .AsNoTracking()
            .AnyAsync(property => property.Id == propertyId.Value && property.Customer!.ManagementCompanyId == managementCompanyId, cancellationToken);

        var propertyBelongsToCustomer = !propertyId.HasValue || !customerId.HasValue || await _dbContext.Properties
            .AsNoTracking()
            .AnyAsync(property => property.Id == propertyId.Value && property.CustomerId == customerId.Value, cancellationToken);

        var unitBelongs = !unitId.HasValue || await _dbContext.Units
            .AsNoTracking()
            .AnyAsync(unit => unit.Id == unitId.Value && unit.Property!.Customer!.ManagementCompanyId == managementCompanyId, cancellationToken);

        var unitBelongsToProperty = !unitId.HasValue || !propertyId.HasValue || await _dbContext.Units
            .AsNoTracking()
            .AnyAsync(unit => unit.Id == unitId.Value && unit.PropertyId == propertyId.Value, cancellationToken);

        var residentBelongs = !residentId.HasValue || await _dbContext.Residents
            .AsNoTracking()
            .AnyAsync(resident => resident.Id == residentId.Value && resident.ManagementCompanyId == managementCompanyId, cancellationToken);

        var residentLinkedToUnit = !residentId.HasValue || !unitId.HasValue || await _dbContext.Leases
            .AsNoTracking()
            .AnyAsync(lease => lease.ResidentId == residentId.Value && lease.UnitId == unitId.Value && lease.IsActive, cancellationToken);

        var vendorBelongs = !vendorId.HasValue || await _dbContext.Vendors
            .AsNoTracking()
            .AnyAsync(vendor => vendor.Id == vendorId.Value && vendor.ManagementCompanyId == managementCompanyId, cancellationToken);

        return new TicketReferenceValidationDalDto
        {
            CategoryExists = categoryExists,
            PriorityExists = priorityExists,
            StatusExists = statusExists,
            CustomerBelongsToCompany = customerBelongs,
            PropertyBelongsToCompany = propertyBelongs,
            PropertyBelongsToCustomer = propertyBelongsToCustomer,
            UnitBelongsToCompany = unitBelongs,
            UnitBelongsToProperty = unitBelongsToProperty,
            ResidentBelongsToCompany = residentBelongs,
            ResidentLinkedToUnit = residentLinkedToUnit,
            VendorBelongsToCompany = vendorBelongs
        };
    }

    public Task<Guid> AddAsync(
        TicketCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ManagementCompanyId = dto.ManagementCompanyId,
            TicketNr = dto.TicketNr,
            Title = dto.Title,
            Description = dto.Description,
            TicketCategoryId = dto.TicketCategoryId,
            TicketStatusId = dto.TicketStatusId,
            TicketPriorityId = dto.TicketPriorityId,
            CustomerId = dto.CustomerId,
            PropertyId = dto.PropertyId,
            UnitId = dto.UnitId,
            ResidentId = dto.ResidentId,
            VendorId = dto.VendorId,
            DueAt = dto.DueAt,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Tickets.Add(ticket);

        return Task.FromResult(ticket.Id);
    }

    public async Task<bool> UpdateAsync(
        TicketUpdateDalDto dto,
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

        ticket.TicketNr = dto.TicketNr;
        ticket.Title.SetTranslation(dto.Title, dto.Culture);
        ticket.Description.SetTranslation(dto.Description, dto.Culture);
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

        return true;
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

    public async Task<bool> DeleteAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(ticket => ticket.Id == ticketId && ticket.ManagementCompanyId == managementCompanyId, cancellationToken);

        if (!exists)
        {
            return false;
        }

        var scheduledWorkIds = await _dbContext.ScheduledWorks
            .Where(work => work.TicketId == ticketId)
            .Select(work => work.Id)
            .ToListAsync(cancellationToken);

        if (scheduledWorkIds.Count > 0)
        {
            await _dbContext.WorkLogs
                .Where(log => scheduledWorkIds.Contains(log.ScheduledWorkId))
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.ScheduledWorks
                .Where(work => scheduledWorkIds.Contains(work.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _dbContext.Tickets
            .Where(ticket => ticket.Id == ticketId && ticket.ManagementCompanyId == managementCompanyId)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    private static IQueryable<TicketOptionDalDto> LookupOptions<TLookup>(IQueryable<TLookup> query)
        where TLookup : BaseEntity, Contracts.ILookUpEntity
    {
        return query
            .AsNoTracking()
            .OrderBy(lookup => lookup.Code)
            .Select(lookup => new TicketOptionDalDto
            {
                Id = lookup.Id,
                Code = lookup.Code,
                Label = lookup.Label.ToString()
            });
    }
}
