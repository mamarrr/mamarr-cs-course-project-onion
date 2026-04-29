using App.Contracts;
using App.Contracts.DAL.Lookups;
using App.DAL.EF.Mappers;
using App.Domain;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public sealed class LookupRepository : ILookupRepository
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
        var statuses = await _dbContext.ManagementCompanyJoinRequestStatuses
            .AsNoTracking()
            .OrderBy(status => status.Code)
            .ToListAsync(cancellationToken);

        return statuses.Select(LookupDalMapper.Map).ToList();
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
        var lookup = await _dbContext.Set<TLookup>()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Code == normalizedCode, cancellationToken);

        return lookup is null ? null : LookupDalMapper.Map(lookup);
    }
}
