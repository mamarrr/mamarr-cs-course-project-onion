using App.Contracts.DAL.Leases;
using App.DAL.EF.Mappers.Leases;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class LeaseRepository :
    BaseRepository<LeaseDalDto, Lease, AppDbContext>,
    ILeaseRepository
{
    private const int MaxSearchResults = 20;

    private readonly AppDbContext _dbContext;

    public LeaseRepository(AppDbContext dbContext, LeaseDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResidentLeaseDalDto>> AllByResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId)
            .Where(entity => entity.Resident!.ManagementCompanyId == managementCompanyId)
            .OrderByDescending(entity => entity.IsActive)
            .ThenByDescending(entity => entity.StartDate)
            .ThenBy(entity => entity.EndDate)
            .Select(entity => new ResidentLeaseDalDto
            {
                LeaseId = entity.Id,
                ResidentId = entity.ResidentId,
                UnitId = entity.UnitId,
                PropertyId = entity.Unit!.PropertyId,
                PropertyName = entity.Unit.Property!.Label.ToString(),
                PropertySlug = entity.Unit.Property.Slug,
                UnitNr = entity.Unit.UnitNr,
                UnitSlug = entity.Unit.Slug,
                LeaseRoleId = entity.LeaseRoleId,
                LeaseRoleCode = entity.LeaseRole!.Code,
                LeaseRoleLabel = entity.LeaseRole.Label.ToString(),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive,
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitLeaseDalDto>> AllByUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.UnitId == unitId)
            .Where(entity => entity.Unit!.PropertyId == propertyId)
            .Where(entity => entity.Resident!.ManagementCompanyId == managementCompanyId)
            .OrderByDescending(entity => entity.IsActive)
            .ThenByDescending(entity => entity.StartDate)
            .ThenBy(entity => entity.EndDate)
            .Select(entity => new UnitLeaseDalDto
            {
                LeaseId = entity.Id,
                ResidentId = entity.ResidentId,
                UnitId = entity.UnitId,
                PropertyId = entity.Unit!.PropertyId,
                ResidentFullName = string.Join(" ", new[] { entity.Resident!.FirstName, entity.Resident.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))),
                ResidentIdCode = entity.Resident.IdCode,
                LeaseRoleId = entity.LeaseRoleId,
                LeaseRoleCode = entity.LeaseRole!.Code,
                LeaseRoleLabel = entity.LeaseRole.Label.ToString(),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive,
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<LeaseDetailsDalDto?> FirstByIdForResidentAsync(
        Guid leaseId,
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return LeaseDetailsQuery()
            .Where(entity => entity.Id == leaseId)
            .Where(entity => entity.ResidentId == residentId)
            .Where(entity => entity.Resident!.ManagementCompanyId == managementCompanyId)
            .Select(entity => new LeaseDetailsDalDto
            {
                LeaseId = entity.Id,
                LeaseRoleId = entity.LeaseRoleId,
                ResidentId = entity.ResidentId,
                UnitId = entity.UnitId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive,
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<LeaseDetailsDalDto?> FirstByIdForUnitAsync(
        Guid leaseId,
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return LeaseDetailsQuery()
            .Where(entity => entity.Id == leaseId)
            .Where(entity => entity.UnitId == unitId)
            .Where(entity => entity.Unit!.PropertyId == propertyId)
            .Where(entity => entity.Resident!.ManagementCompanyId == managementCompanyId)
            .Select(entity => new LeaseDetailsDalDto
            {
                LeaseId = entity.Id,
                LeaseRoleId = entity.LeaseRoleId,
                ResidentId = entity.ResidentId,
                UnitId = entity.UnitId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                IsActive = entity.IsActive,
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> LeaseRoleExistsAsync(
        Guid leaseRoleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseRoles
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == leaseRoleId, cancellationToken);
    }

    public Task<bool> UnitExistsInCompanyAsync(
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

    public Task<bool> ResidentExistsInCompanyAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Residents
            .AsNoTracking()
            .AnyAsync(
                entity => entity.Id == residentId && entity.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> PropertyExistsInCompanyAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Properties
            .AsNoTracking()
            .AnyAsync(
                entity => entity.Id == propertyId && entity.Customer!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public Task<bool> HasOverlappingActiveLeaseAsync(
        Guid residentId,
        Guid unitId,
        DateOnly startDate,
        Guid? exceptLeaseId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId && entity.UnitId == unitId && entity.IsActive)
            .Where(entity => !exceptLeaseId.HasValue || entity.Id != exceptLeaseId.Value)
            .AnyAsync(entity => !entity.EndDate.HasValue || entity.EndDate.Value >= startDate, cancellationToken);
    }

    public Task<LeaseDalDto> AddAsync(
        LeaseCreateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = dto.UnitId,
            ResidentId = dto.ResidentId,
            LeaseRoleId = dto.LeaseRoleId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : new LangStr(dto.Notes.Trim())
        };

        _dbContext.Leases.Add(lease);
        return Task.FromResult(new LeaseDalDto
        {
            Id = lease.Id,
            UnitId = lease.UnitId,
            ResidentId = lease.ResidentId,
            LeaseRoleId = lease.LeaseRoleId,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            IsActive = lease.IsActive,
            Notes = lease.Notes == null ? null : lease.Notes.ToString()
        });
    }

    public async Task<bool> UpdateForResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        LeaseUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.LeaseId
                          && entity.ResidentId == residentId
                          && entity.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (lease is null)
        {
            return false;
        }

        ApplyUpdate(lease, dto);
        return true;
    }

    public async Task<bool> UpdateForUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        LeaseUpdateDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.LeaseId
                          && entity.UnitId == unitId
                          && entity.Unit!.PropertyId == propertyId
                          && entity.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (lease is null)
        {
            return false;
        }

        ApplyUpdate(lease, dto);
        return true;
    }

    public async Task<bool> DeleteForResidentAsync(
        Guid leaseId,
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == leaseId
                          && entity.ResidentId == residentId
                          && entity.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (lease is null)
        {
            return false;
        }

        _dbContext.Leases.Remove(lease);
        return true;
    }

    public async Task<bool> DeleteForUnitAsync(
        Guid leaseId,
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == leaseId
                          && entity.UnitId == unitId
                          && entity.Unit!.PropertyId == propertyId
                          && entity.Resident!.ManagementCompanyId == managementCompanyId,
                cancellationToken);

        if (lease is null)
        {
            return false;
        }

        _dbContext.Leases.Remove(lease);
        return true;
    }

    public async Task<IReadOnlyList<LeasePropertySearchItemDalDto>> SearchPropertiesAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<LeasePropertySearchItemDalDto>();
        }

        var candidates = await _dbContext.Properties
            .AsNoTracking()
            .Where(entity => entity.Customer!.ManagementCompanyId == managementCompanyId)
            .OrderBy(entity => entity.Slug)
            .ThenBy(entity => entity.AddressLine)
            .Take(250)
            .Select(entity => new
            {
                PropertyId = entity.Id,
                entity.CustomerId,
                PropertySlug = entity.Slug,
                entity.Label,
                CustomerSlug = entity.Customer!.Slug,
                CustomerName = entity.Customer.Name,
                entity.AddressLine,
                entity.City,
                entity.PostalCode
            })
            .ToListAsync(cancellationToken);

        static bool ContainsCI(string? value, string term)
            => !string.IsNullOrWhiteSpace(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        return candidates
            .Where(entity =>
                ContainsCI(entity.Label.ToString(), normalizedSearch) ||
                ContainsCI(entity.AddressLine, normalizedSearch) ||
                ContainsCI(entity.City, normalizedSearch) ||
                ContainsCI(entity.PostalCode, normalizedSearch) ||
                ContainsCI(entity.CustomerName, normalizedSearch) ||
                ContainsCI(entity.PropertySlug, normalizedSearch))
            .Take(MaxSearchResults)
            .Select(entity => new LeasePropertySearchItemDalDto
            {
                PropertyId = entity.PropertyId,
                CustomerId = entity.CustomerId,
                PropertySlug = entity.PropertySlug,
                PropertyName = entity.Label.ToString(),
                CustomerSlug = entity.CustomerSlug,
                CustomerName = entity.CustomerName,
                AddressLine = entity.AddressLine,
                City = entity.City,
                PostalCode = entity.PostalCode
            })
            .ToList();
    }

    public async Task<IReadOnlyList<LeaseUnitOptionDalDto>> ListUnitsForPropertyAsync(
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
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaseResidentSearchItemDalDto>> SearchResidentsAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<LeaseResidentSearchItemDalDto>();
        }

        var pattern = $"%{normalizedSearch}%";

        return await _dbContext.Residents
            .AsNoTracking()
            .Where(entity => entity.ManagementCompanyId == managementCompanyId)
            .Where(entity =>
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.FirstName, pattern) ||
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.LastName, pattern) ||
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.FirstName + " " + entity.LastName, pattern) ||
                Microsoft.EntityFrameworkCore.EF.Functions.ILike(entity.IdCode, pattern))
            .OrderBy(entity => entity.FirstName)
            .ThenBy(entity => entity.LastName)
            .ThenBy(entity => entity.IdCode)
            .Take(MaxSearchResults)
            .Select(entity => new LeaseResidentSearchItemDalDto
            {
                ResidentId = entity.Id,
                FullName = string.Join(" ", new[] { entity.FirstName, entity.LastName }.Where(value => !string.IsNullOrWhiteSpace(value))),
                IdCode = entity.IdCode,
                IsActive = entity.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeaseRoleOptionDalDto>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaseRoles
            .AsNoTracking()
            .OrderBy(entity => entity.Label)
            .ThenBy(entity => entity.Code)
            .Select(entity => new LeaseRoleOptionDalDto
            {
                LeaseRoleId = entity.Id,
                Code = entity.Code,
                Label = entity.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Lease> LeaseDetailsQuery()
    {
        return _dbContext.Leases.AsNoTracking();
    }

    private void ApplyUpdate(Lease lease, LeaseUpdateDalDto dto)
    {
        lease.LeaseRoleId = dto.LeaseRoleId;
        lease.StartDate = dto.StartDate;
        lease.EndDate = dto.EndDate;
        lease.IsActive = dto.IsActive;

        if (string.IsNullOrWhiteSpace(dto.Notes))
        {
            lease.Notes = null;
            _dbContext.Entry(lease).Property(entity => entity.Notes).IsModified = true;
            return;
        }

        var normalizedNotes = dto.Notes.Trim();
        if (lease.Notes is null)
        {
            lease.Notes = new LangStr(normalizedNotes);
            _dbContext.Entry(lease).Property(entity => entity.Notes).IsModified = true;
            return;
        }

        lease.Notes.SetTranslation(normalizedNotes);
        _dbContext.Entry(lease).Property(entity => entity.Notes).IsModified = true;
    }
}
