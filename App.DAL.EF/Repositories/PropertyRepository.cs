using App.Contracts.DAL.Properties;
using App.DAL.EF.Mappers.Properties;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class PropertyRepository :
    BaseRepository<PropertyDalDto, Property, AppDbContext>,
    IPropertyRepository
{
    private readonly AppDbContext _dbContext;
    private readonly PropertyDalMapper _mapper;

    public PropertyRepository(AppDbContext dbContext, PropertyDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PropertyListItemDalDto>> AllByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Include(property => property.Customer)
            .Include(property => property.PropertyType)
            .Where(property => property.CustomerId == customerId)
            .OrderBy(property => property.Label)
            .ThenBy(property => property.Id)
            .ToListAsync(cancellationToken);

        return properties.Select(_mapper.MapListItem).ToList();
    }

    public async Task<IReadOnlyList<PropertyTypeOptionDalDto>> AllPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PropertyTypes
            .AsNoTracking()
            .OrderBy(propertyType => propertyType.Code)
            .Select(propertyType => new PropertyTypeOptionDalDto
            {
                Id = propertyType.Id,
                Code = propertyType.Code,
                Label = propertyType.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyWorkspaceDalDto?> FirstWorkspaceByCustomerAndSlugAsync(
        Guid customerId,
        string propertySlug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = propertySlug.Trim();

        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.CustomerId == customerId && property.Slug == normalizedSlug)
            .FirstOrDefaultAsync(cancellationToken);

        return property is null ? null : _mapper.MapWorkspace(property);
    }

    public async Task<PropertyProfileDalDto?> FindProfileAsync(
        Guid propertyId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Include(entity => entity.Customer)!
            .ThenInclude(customer => customer!.ManagementCompany)
            .Include(entity => entity.PropertyType)
            .Where(property => property.Id == propertyId && property.CustomerId == customerId)
            .FirstOrDefaultAsync(cancellationToken);

        return property is null ? null : _mapper.MapProfile(property);
    }

    public async Task<bool> PropertyTypeExistsAsync(
        Guid propertyTypeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PropertyTypes
            .AsNoTracking()
            .AnyAsync(propertyType => propertyType.Id == propertyTypeId, cancellationToken);
    }

    public async Task<bool> SlugExistsForCustomerAsync(
        Guid customerId,
        string slug,
        Guid? exceptPropertyId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        return await _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.CustomerId == customerId)
            .Where(property => exceptPropertyId == null || property.Id != exceptPropertyId.Value)
            .AnyAsync(property => property.Slug.ToLower() == normalizedSlug, cancellationToken);
    }

    public Task<PropertyDalDto> AddAsync(
        PropertyCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var property = new Property
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            PropertyTypeId = dto.PropertyTypeId,
            Label = dto.Name,
            Slug = dto.Slug,
            AddressLine = dto.AddressLine,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : new LangStr(dto.Notes.Trim()),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Properties.Add(property);

        return Task.FromResult(new PropertyDalDto
        {
            Id = property.Id,
            CustomerId = property.CustomerId,
            Name = property.Label.ToString(),
            Slug = property.Slug,
            IsActive = property.IsActive
        });
    }

    public async Task UpdateProfileAsync(
        PropertyUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.CustomerId == dto.CustomerId,
                cancellationToken);

        if (property is null)
        {
            return;
        }

        property.Label.SetTranslation(dto.Name);
        _dbContext.Entry(property).Property(entity => entity.Label).IsModified = true;

        property.AddressLine = dto.AddressLine;
        property.City = dto.City;
        property.PostalCode = dto.PostalCode;
        property.IsActive = dto.IsActive;

        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            property.Notes = null;
        }
        else if (property.Notes is null)
        {
            property.Notes = new LangStr(dto.Notes.Trim());
            _dbContext.Entry(property).Property(entity => entity.Notes).IsModified = true;
        }
        else
        {
            property.Notes.SetTranslation(dto.Notes.Trim());
            _dbContext.Entry(property).Property(entity => entity.Notes).IsModified = true;
        }
    }

    public async Task<bool> DeleteAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(entity => entity.Id == propertyId && entity.CustomerId == customerId)
            .Select(entity => new { entity.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (property is null)
        {
            return false;
        }

        var unitIds = await _dbContext.Units
            .Where(unit => unit.PropertyId == property.Id)
            .Select(unit => unit.Id)
            .ToListAsync(cancellationToken);

        var ticketIds = await _dbContext.Tickets
            .Where(ticket => (ticket.PropertyId.HasValue && ticket.PropertyId.Value == property.Id)
                             || (ticket.UnitId.HasValue && unitIds.Contains(ticket.UnitId.Value)))
            .Where(ticket => ticket.ManagementCompanyId == managementCompanyId)
            .Select(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

        await DeleteTicketsAsync(ticketIds, cancellationToken);

        await _dbContext.Leases
            .Where(lease => unitIds.Contains(lease.UnitId))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(unit => unitIds.Contains(unit.Id))
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Properties
            .Where(entity => entity.Id == property.Id)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    private async Task DeleteTicketsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return;
        }

        var scheduledWorkIds = await _dbContext.ScheduledWorks
            .Where(scheduledWork => ticketIds.Contains(scheduledWork.TicketId))
            .Select(scheduledWork => scheduledWork.Id)
            .ToListAsync(cancellationToken);

        if (scheduledWorkIds.Count > 0)
        {
            await _dbContext.WorkLogs
                .Where(workLog => scheduledWorkIds.Contains(workLog.ScheduledWorkId))
                .ExecuteDeleteAsync(cancellationToken);

            await _dbContext.ScheduledWorks
                .Where(scheduledWork => scheduledWorkIds.Contains(scheduledWork.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _dbContext.Tickets
            .Where(ticket => ticketIds.Contains(ticket.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
