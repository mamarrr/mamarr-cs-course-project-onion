using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.ResidentId == residentId)
            .Where(entity => entity.Resident!.ManagementCompanyId == managementCompanyId)
            .OrderByDescending(entity => entity.StartDate <= today && (!entity.EndDate.HasValue || entity.EndDate.Value >= today))
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _dbContext.Leases
            .AsNoTracking()
            .Where(entity => entity.UnitId == unitId)
            .Where(entity => entity.Unit!.PropertyId == propertyId)
            .Where(entity => entity.Resident!.ManagementCompanyId == managementCompanyId)
            .OrderByDescending(entity => entity.StartDate <= today && (!entity.EndDate.HasValue || entity.EndDate.Value >= today))
            .ThenByDescending(entity => entity.StartDate)
            .ThenBy(entity => entity.EndDate)
            .Select(entity => new UnitLeaseDalDto
            {
                LeaseId = entity.Id,
                ResidentId = entity.ResidentId,
                UnitId = entity.UnitId,
                PropertyId = entity.Unit!.PropertyId,
                ResidentFullName = (entity.Resident!.FirstName + " " + entity.Resident.LastName).Trim(),
                ResidentIdCode = entity.Resident.IdCode,
                LeaseRoleId = entity.LeaseRoleId,
                LeaseRoleCode = entity.LeaseRole!.Code,
                LeaseRoleLabel = entity.LeaseRole.Label.ToString(),
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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
                Notes = entity.Notes == null ? null : entity.Notes.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);
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
            .Where(entity => entity.ResidentId == residentId && entity.UnitId == unitId)
            .Where(entity => !exceptLeaseId.HasValue || entity.Id != exceptLeaseId.Value)
            .AnyAsync(entity => !entity.EndDate.HasValue || entity.EndDate.Value >= startDate, cancellationToken);
    }

    public async Task<bool> UpdateForResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        LeaseDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id
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
        LeaseDalDto dto,
        CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.Leases
            .AsTracking()
            .FirstOrDefaultAsync(
                entity => entity.Id == dto.Id
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

    private IQueryable<Lease> LeaseDetailsQuery()
    {
        return _dbContext.Leases.AsNoTracking();
    }

    private void ApplyUpdate(Lease lease, LeaseDalDto dto)
    {
        lease.LeaseRoleId = dto.LeaseRoleId;
        lease.StartDate = dto.StartDate;
        lease.EndDate = dto.EndDate;
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
