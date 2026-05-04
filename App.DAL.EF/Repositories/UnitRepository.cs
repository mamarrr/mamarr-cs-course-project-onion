using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
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

    public Task<bool> ExistsInCompanyAsync(
        Guid unitId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Units
            .AnyAsync(
                entity => entity.Id == unitId
                          && entity.Property!.Customer!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeaseUnitOptionDalDto>> ListForLeaseAssignmentAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Units
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
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);
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
            IsActive = unit.IsActive
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
        var deleted = await _dbContext.Units
            .Where(entity => entity.Id == unitId
                             && entity.PropertyId == propertyId
                             && entity.Property!.Customer!.ManagementCompanyId == managementCompanyId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    public async Task<IReadOnlyList<Guid>> AllIdsByPropertyIdsAsync(
        IReadOnlyCollection<Guid> propertyIds,
        CancellationToken cancellationToken = default)
    {
        if (propertyIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        return await _dbContext.Units
            .Where(unit => propertyIds.Contains(unit.PropertyId))
            .Select(unit => unit.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByIdsAsync(
        IReadOnlyCollection<Guid> unitIds,
        CancellationToken cancellationToken = default)
    {
        if (unitIds.Count == 0)
        {
            return;
        }

        await _dbContext.Units
            .Where(unit => unitIds.Contains(unit.Id))
            .ExecuteDeleteAsync(cancellationToken);
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
