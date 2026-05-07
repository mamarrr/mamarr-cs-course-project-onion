using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Lookups;
using App.DAL.DTO.Properties;
using App.DAL.DTO.Tickets;
using App.Domain;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

/// <summary>
/// Since LookupRepository does not inherit from BaseRepository, then if a lookupentity has CreatedAt field and the
/// repository needs to add a new lookupEntity to the database, then the adding method has to deal with CreatedAt Datetime
/// correct creation.
/// </summary>
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

    public async Task<LookupDalDto?> FindManagementCompanyRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .Select(role => new LookupDalDto
            {
                Id = role.Id,
                Code = role.Code,
                Label = role.Label.ToString()
            })
            .FirstOrDefaultAsync(role => role.Id == roleId, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ManagementCompanyRoles
            .AsNoTracking()
            .OrderBy(role => role.Code)
            .Select(role => new LookupDalDto
            {
                Id = role.Id,
                Code = role.Code,
                Label = role.Label.ToString()
            })
            .ToListAsync(cancellationToken);
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
            .AsNoTracking()
            .AnyAsync(entity => entity.Id == leaseRoleId, cancellationToken);
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

    public Task<LookupDalDto?> FindPropertyTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<PropertyType>(code, cancellationToken);
    }

    public Task<bool> PropertyTypeExistsAsync(
        Guid propertyTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PropertyTypes
            .AsNoTracking()
            .AnyAsync(propertyType => propertyType.Id == propertyTypeId, cancellationToken);
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

    public Task<LookupDalDto?> FindContactTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return FindByCodeAsync<ContactType>(code, cancellationToken);
    }

    public Task<bool> ContactTypeExistsAsync(
        Guid contactTypeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ContactTypes
            .AsNoTracking()
            .AnyAsync(contactType => contactType.Id == contactTypeId, cancellationToken);
    }

    public async Task<IReadOnlyList<LookupDalDto>> AllContactTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ContactTypes
            .AsNoTracking()
            .OrderBy(contactType => contactType.Code)
            .Select(contactType => new LookupDalDto
            {
                Id = contactType.Id,
                Code = contactType.Code,
                Label = contactType.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    public Task<TicketOptionDalDto?> FindTicketStatusByCodeAsync(
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

    public Task<TicketOptionDalDto?> FindTicketStatusByIdAsync(
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

    public async Task<IReadOnlyList<TicketOptionDalDto>> AllTicketStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        return await TicketLookupOptions(_dbContext.TicketStatuses).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> AllTicketPrioritiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await TicketLookupOptions(_dbContext.TicketPriorities).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TicketOptionDalDto>> AllTicketCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await TicketLookupOptions(_dbContext.TicketCategories).ToListAsync(cancellationToken);
    }

    public Task<bool> TicketCategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TicketCategories
            .AsNoTracking()
            .AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    public Task<bool> TicketPriorityExistsAsync(
        Guid priorityId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TicketPriorities
            .AsNoTracking()
            .AnyAsync(priority => priority.Id == priorityId, cancellationToken);
    }

    public Task<bool> TicketStatusExistsAsync(
        Guid statusId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TicketStatuses
            .AsNoTracking()
            .AnyAsync(status => status.Id == statusId, cancellationToken);
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

    private static IQueryable<TicketOptionDalDto> TicketLookupOptions<TLookup>(IQueryable<TLookup> query)
        where TLookup : BaseEntity, ILookUpEntity
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
