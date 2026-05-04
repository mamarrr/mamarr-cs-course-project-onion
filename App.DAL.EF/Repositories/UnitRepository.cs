using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Units;
using App.DAL.EF.Mappers.Units;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class UnitRepository :
    BaseRepository<UnitDalDto, Unit, AppDbContext>,
    IUnitRepository
{
    private readonly AppDbContext _dbContext;

    public UnitRepository(AppDbContext dbContext, UnitDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<UnitDashboardDalDto?> FirstDashboardAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken = default)
    {
        var unit = await BaseScopedUnitQuery(companySlug, customerSlug, propertySlug, unitSlug)
            .Select(entity => new UnitDashboardDalDto
            {
                Id = entity.Id,
                PropertyId = entity.PropertyId,
                CustomerId = entity.Property!.CustomerId,
                ManagementCompanyId = entity.Property.Customer!.ManagementCompanyId,
                CompanySlug = entity.Property.Customer.ManagementCompany!.Slug,
                CompanyName = entity.Property.Customer.ManagementCompany.Name,
                CustomerSlug = entity.Property.Customer.Slug,
                CustomerName = entity.Property.Customer.Name,
                PropertySlug = entity.Property.Slug,
                PropertyName = entity.Property.Label.ToString(),
                UnitNr = entity.UnitNr,
                Slug = entity.Slug
            })
            .FirstOrDefaultAsync(cancellationToken);

        return unit;
    }

    public async Task<UnitProfileDalDto?> FirstProfileAsync(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug,
        CancellationToken cancellationToken = default)
    {
        var unit = await BaseScopedUnitQuery(companySlug, customerSlug, propertySlug, unitSlug)
            .Select(entity => new UnitProfileDalDto
            {
                Id = entity.Id,
                PropertyId = entity.PropertyId,
                CustomerId = entity.Property!.CustomerId,
                ManagementCompanyId = entity.Property.Customer!.ManagementCompanyId,
                CompanySlug = entity.Property.Customer.ManagementCompany!.Slug,
                CompanyName = entity.Property.Customer.ManagementCompany.Name,
                CustomerSlug = entity.Property.Customer.Slug,
                CustomerName = entity.Property.Customer.Name,
                PropertySlug = entity.Property.Slug,
                PropertyName = entity.Property.Label.ToString(),
                UnitNr = entity.UnitNr,
                Slug = entity.Slug,
                FloorNr = entity.FloorNr,
                SizeM2 = entity.SizeM2,
                Notes = entity.Notes == null ? null : entity.Notes.ToString(),
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return unit;
    }

    public async Task<UnitProfileDalDto?> FindProfileAsync(
        Guid unitId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.Id == unitId && unit.PropertyId == propertyId)
            .Select(entity => new UnitProfileDalDto
            {
                Id = entity.Id,
                PropertyId = entity.PropertyId,
                CustomerId = entity.Property!.CustomerId,
                ManagementCompanyId = entity.Property.Customer!.ManagementCompanyId,
                CompanySlug = entity.Property.Customer.ManagementCompany!.Slug,
                CompanyName = entity.Property.Customer.ManagementCompany.Name,
                CustomerSlug = entity.Property.Customer.Slug,
                CustomerName = entity.Property.Customer.Name,
                PropertySlug = entity.Property.Slug,
                PropertyName = entity.Property.Label.ToString(),
                UnitNr = entity.UnitNr,
                Slug = entity.Slug,
                FloorNr = entity.FloorNr,
                SizeM2 = entity.SizeM2,
                Notes = entity.Notes == null ? null : entity.Notes.ToString(),
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return unit;
    }

    public async Task<IReadOnlyList<UnitListItemDalDto>> AllByPropertyAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var units = await _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId)
            .OrderBy(unit => unit.UnitNr)
            .ThenBy(unit => unit.FloorNr)
            .ThenBy(unit => unit.Id)
            .Select(unit => new UnitListItemDalDto
            {
                Id = unit.Id,
                PropertyId = unit.PropertyId,
                UnitNr = unit.UnitNr,
                Slug = unit.Slug,
                FloorNr = unit.FloorNr,
                SizeM2 = unit.SizeM2
            })
            .ToListAsync(cancellationToken);

        return units;
    }

    public async Task<IReadOnlyList<string>> AllSlugsByPropertyWithPrefixAsync(
        Guid propertyId,
        string slugPrefix,
        CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = slugPrefix.Trim();

        return await _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId && unit.Slug.StartsWith(normalizedPrefix))
            .Select(unit => unit.Slug)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UnitSlugExistsForPropertyAsync(
        Guid propertyId,
        string slug,
        Guid? exceptUnitId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        return await _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId)
            .Where(unit => exceptUnitId == null || unit.Id != exceptUnitId.Value)
            .AnyAsync(unit => unit.Slug.ToLower() == normalizedSlug, cancellationToken);
    }

    public Task<UnitDalDto> AddAsync(
        UnitCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            PropertyId = dto.PropertyId,
            UnitNr = dto.UnitNr,
            Slug = dto.Slug,
            FloorNr = dto.FloorNr,
            SizeM2 = dto.SizeM2,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : new LangStr(dto.Notes.Trim()),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Units.Add(unit);

        return Task.FromResult(new UnitDalDto
        {
            Id = unit.Id,
            PropertyId = unit.PropertyId,
            UnitNr = unit.UnitNr,
            Slug = unit.Slug,
            FloorNr = unit.FloorNr,
            SizeM2 = unit.SizeM2,
            Notes = unit.Notes?.ToString(),
            IsActive = unit.IsActive,
            CreatedAt = unit.CreatedAt
        });
    }

    public async Task UpdateAsync(
        UnitUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var unit = await _dbContext.Units
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.PropertyId == dto.PropertyId,
                cancellationToken);

        if (unit is null)
        {
            return;
        }

        unit.UnitNr = dto.UnitNr;
        unit.FloorNr = dto.FloorNr;
        unit.SizeM2 = dto.SizeM2;
        unit.IsActive = dto.IsActive;

        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            unit.Notes = null;
            _dbContext.Entry(unit).Property(entity => entity.Notes).IsModified = true;
        }
        else if (unit.Notes is null)
        {
            unit.Notes = new LangStr(dto.Notes.Trim());
            _dbContext.Entry(unit).Property(entity => entity.Notes).IsModified = true;
        }
        else
        {
            unit.Notes.SetTranslation(dto.Notes.Trim());
            _dbContext.Entry(unit).Property(entity => entity.Notes).IsModified = true;
        }
    }

    public async Task<bool> DeleteAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var unit = await _dbContext.Units
            .AsNoTracking()
            .Where(entity => entity.Id == unitId && entity.PropertyId == propertyId)
            .Select(entity => new { entity.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (unit is null)
        {
            return false;
        }

        var ticketIds = await _dbContext.Tickets
            .Where(ticket => ticket.UnitId == unit.Id && ticket.ManagementCompanyId == managementCompanyId)
            .Select(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

        await DeleteTicketsAsync(ticketIds, cancellationToken);

        await _dbContext.Leases
            .Where(lease => lease.UnitId == unit.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.Units
            .Where(entity => entity.Id == unit.Id)
            .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    private IQueryable<Unit> BaseScopedUnitQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        string unitSlug)
    {
        var normalizedCompanySlug = companySlug.Trim();
        var normalizedCustomerSlug = customerSlug.Trim();
        var normalizedPropertySlug = propertySlug.Trim();
        var normalizedUnitSlug = unitSlug.Trim();

        return _dbContext.Units
            .AsNoTracking()
            .Include(entity => entity.Property)!
            .ThenInclude(property => property!.Customer)!
            .ThenInclude(customer => customer!.ManagementCompany)
            .Where(unit => unit.Property!.Customer!.ManagementCompany!.Slug == normalizedCompanySlug)
            .Where(unit => unit.Property!.Customer!.Slug == normalizedCustomerSlug)
            .Where(unit => unit.Property!.Slug == normalizedPropertySlug)
            .Where(unit => unit.Slug == normalizedUnitSlug);
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
