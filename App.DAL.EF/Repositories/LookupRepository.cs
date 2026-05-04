using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Lookups;
using App.Domain;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly AppDbContext _dbContext;

    public LookupRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LookupDalDto?> FindManagementCompanyJoinRequestStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<ManagementCompanyJoinRequestStatus>(code, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyJoinRequestStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyJoinRequestStatuses
            .AsNoTracking()
            .OrderBy(status => status.Code)
            .Select(status => new LookupDalDto
            {
                Id = status.Id,
                Code = status.Code,
                Label = status.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<LookupDalDto?> FindManagementCompanyRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<ManagementCompanyRole>(code, cancellationToken);
    }

    public Task<LookupDalDto?> FindCustomerRepresentativeRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<CustomerRepresentativeRole>(code, cancellationToken);
    }

    public Task<LookupDalDto?> FindLeaseRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<LeaseRole>(code, cancellationToken);
    }

    public Task<bool> LeaseRoleExistsAsync(
        Guid leaseRoleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.LeaseRoles
            .AnyAsync(entity => entity.Id == leaseRoleId, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaseRoleOptionDalDto>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaseRoles
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

    public Task<LookupDalDto?> FindPropertyTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<PropertyType>(code, cancellationToken);
    }

    public Task<LookupDalDto?> FindContactTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<ContactType>(code, cancellationToken);
    }

    private async Task<LookupDalDto?> FindByCodeAsync<TLookup>(
        string code,
        CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim();
        return await _dbContext.Set<TLookup>()
            .AsNoTracking()
            .Where(entity => entity.Code == normalizedCode)
            .Select(entity => new LookupDalDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Label = entity.Label.ToString()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
