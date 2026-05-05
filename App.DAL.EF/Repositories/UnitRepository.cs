using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Tickets;
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

    public Task<bool> ExistsInCompanyAsync(
        Guid unitId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Units
            .AsNoTracking()
            .AnyAsync(
                entity => entity.Id == unitId
                          && entity.Property!.Customer!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> ExistsInPropertyAsync(
        Guid unitId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Units
            .AsNoTracking()
            .AnyAsync(
                unit => unit.Id == unitId && unit.PropertyId == propertyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? propertyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Units
            .AsNoTracking()
            .Where(unit => unit.Property!.Customer!.ManagementCompanyId == managementCompanyId);

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

    public async Task<IReadOnlyList<LeaseUnitOptionDalDto>> ListForLeaseAssignmentAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Units
            .AsNoTracking()
            .Where(entity => entity.PropertyId == propertyId)
            .Where(entity => entity.Property!.Customer!.ManagementCompanyId == managementCompanyId)
            .OrderBy(entity => entity.UnitNr)
            .ThenBy(entity => entity.FloorNr)
            .ThenBy(entity => entity.Id)
            .Select(entity => new LeaseUnitOptionDalDto
            {
                UnitId = entity.Id,
                UnitSlug = entity.Slug,
                UnitNr = entity.UnitNr,
                FloorNr = entity.FloorNr,
                
            })
            .ToListAsync(cancellationToken);
    }

    public override async Task<UnitDalDto> UpdateAsync(
        UnitDalDto dto,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var propertyId = parentId == default ? dto.PropertyId : parentId;

        var unit = await _dbContext.Units
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id && entity.PropertyId == propertyId,
                cancellationToken);

        if (unit is null)
        {
            throw new ApplicationException($"Unit with id {dto.Id} was not found.");
        }

        unit.UnitNr = dto.UnitNr;
        unit.Slug = dto.Slug;
        unit.FloorNr = dto.FloorNr;
        unit.SizeM2 = dto.SizeM2;
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

        return Mapper.Map(unit)!;
    }

    public override async Task RemoveAsync(
        Guid id,
        Guid parentId = default,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Units.AsTracking();
        if (parentId != default)
        {
            query = query.Where(unit => unit.PropertyId == parentId);
        }

        var unit = await query.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (unit is not null)
        {
            _dbContext.Units.Remove(unit);
        }
    }

    public async Task<bool> HasDeleteDependenciesAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var leaseExists = await _dbContext.Leases
            .AsNoTracking()
            .AnyAsync(
                lease => lease.UnitId == unitId
                         && lease.Unit!.PropertyId == propertyId
                         && lease.Unit.Property!.Customer!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (leaseExists)
        {
            return true;
        }

        return await _dbContext.Tickets
            .AsNoTracking()
            .AnyAsync(
                ticket => ticket.UnitId == unitId
                          && ticket.ManagementCompanyId == managementCompanyId
                          && ticket.Unit!.PropertyId == propertyId,
                cancellationToken);
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
}
