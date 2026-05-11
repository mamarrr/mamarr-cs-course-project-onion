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

    public Task<TicketOptionDalDto?> FindWorkStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        return _dbContext.WorkStatuses
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

    public Task<TicketOptionDalDto?> FindWorkStatusByIdAsync(
        Guid statusId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkStatuses
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

    public async Task<IReadOnlyList<TicketOptionDalDto>> AllWorkStatusesAsync(
        CancellationToken cancellationToken = default)
    {
        return await TicketLookupOptions(_dbContext.WorkStatuses).ToListAsync(cancellationToken);
    }

    public Task<bool> WorkStatusExistsAsync(
        Guid statusId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkStatuses
            .AsNoTracking()
            .AnyAsync(status => status.Id == statusId, cancellationToken);
    }

    public Task<IReadOnlyList<LookupItemDalDto>> GetLookupItemsAsync(
        LookupTable table,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => GetItemsAsync<PropertyType>(cancellationToken),
            LookupTable.TicketCategory => GetItemsAsync<TicketCategory>(cancellationToken),
            LookupTable.TicketPriority => GetItemsAsync<TicketPriority>(cancellationToken),
            LookupTable.TicketStatus => GetItemsAsync<TicketStatus>(cancellationToken),
            LookupTable.WorkStatus => GetItemsAsync<WorkStatus>(cancellationToken),
            LookupTable.ContactType => GetItemsAsync<ContactType>(cancellationToken),
            LookupTable.ManagementCompanyRole => GetItemsAsync<ManagementCompanyRole>(cancellationToken),
            _ => Task.FromResult<IReadOnlyList<LookupItemDalDto>>([])
        };
    }

    public Task<LookupItemDalDto?> FindLookupItemAsync(
        LookupTable table,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => FindItemAsync<PropertyType>(id, cancellationToken),
            LookupTable.TicketCategory => FindItemAsync<TicketCategory>(id, cancellationToken),
            LookupTable.TicketPriority => FindItemAsync<TicketPriority>(id, cancellationToken),
            LookupTable.TicketStatus => FindItemAsync<TicketStatus>(id, cancellationToken),
            LookupTable.WorkStatus => FindItemAsync<WorkStatus>(id, cancellationToken),
            LookupTable.ContactType => FindItemAsync<ContactType>(id, cancellationToken),
            LookupTable.ManagementCompanyRole => FindItemAsync<ManagementCompanyRole>(id, cancellationToken),
            _ => Task.FromResult<LookupItemDalDto?>(null)
        };
    }

    public Task<bool> CodeExistsAsync(
        LookupTable table,
        string code,
        Guid? exceptId = null,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => CodeExistsAsync<PropertyType>(code, exceptId, cancellationToken),
            LookupTable.TicketCategory => CodeExistsAsync<TicketCategory>(code, exceptId, cancellationToken),
            LookupTable.TicketPriority => CodeExistsAsync<TicketPriority>(code, exceptId, cancellationToken),
            LookupTable.TicketStatus => CodeExistsAsync<TicketStatus>(code, exceptId, cancellationToken),
            LookupTable.WorkStatus => CodeExistsAsync<WorkStatus>(code, exceptId, cancellationToken),
            LookupTable.ContactType => CodeExistsAsync<ContactType>(code, exceptId, cancellationToken),
            LookupTable.ManagementCompanyRole => CodeExistsAsync<ManagementCompanyRole>(code, exceptId, cancellationToken),
            _ => Task.FromResult(false)
        };
    }

    public async Task<LookupItemDalDto> CreateLookupItemAsync(
        LookupTable table,
        string code,
        string label,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => await CreateItemAsync<PropertyType>(code, label, cancellationToken),
            LookupTable.TicketCategory => await CreateItemAsync<TicketCategory>(code, label, cancellationToken),
            LookupTable.TicketPriority => await CreateItemAsync<TicketPriority>(code, label, cancellationToken),
            LookupTable.TicketStatus => await CreateItemAsync<TicketStatus>(code, label, cancellationToken),
            LookupTable.WorkStatus => await CreateItemAsync<WorkStatus>(code, label, cancellationToken),
            LookupTable.ContactType => await CreateItemAsync<ContactType>(code, label, cancellationToken),
            LookupTable.ManagementCompanyRole => await CreateItemAsync<ManagementCompanyRole>(code, label, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, null)
        };
    }

    public async Task<LookupItemDalDto?> UpdateLookupItemAsync(
        LookupTable table,
        Guid id,
        string code,
        string label,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => await UpdateItemAsync<PropertyType>(id, code, label, cancellationToken),
            LookupTable.TicketCategory => await UpdateItemAsync<TicketCategory>(id, code, label, cancellationToken),
            LookupTable.TicketPriority => await UpdateItemAsync<TicketPriority>(id, code, label, cancellationToken),
            LookupTable.TicketStatus => await UpdateItemAsync<TicketStatus>(id, code, label, cancellationToken),
            LookupTable.WorkStatus => await UpdateItemAsync<WorkStatus>(id, code, label, cancellationToken),
            LookupTable.ContactType => await UpdateItemAsync<ContactType>(id, code, label, cancellationToken),
            LookupTable.ManagementCompanyRole => await UpdateItemAsync<ManagementCompanyRole>(id, code, label, cancellationToken),
            _ => null
        };
    }

    public Task<bool> IsLookupInUseAsync(
        LookupTable table,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => _dbContext.Properties.AsNoTracking().AnyAsync(entity => entity.PropertyTypeId == id, cancellationToken),
            LookupTable.TicketCategory => _dbContext.Tickets.AsNoTracking().AnyAsync(entity => entity.TicketCategoryId == id, cancellationToken),
            LookupTable.TicketPriority => _dbContext.Tickets.AsNoTracking().AnyAsync(entity => entity.TicketPriorityId == id, cancellationToken),
            LookupTable.TicketStatus => _dbContext.Tickets.AsNoTracking().AnyAsync(entity => entity.TicketStatusId == id, cancellationToken),
            LookupTable.WorkStatus => _dbContext.ScheduledWorks.AsNoTracking().AnyAsync(entity => entity.WorkStatusId == id, cancellationToken),
            LookupTable.ContactType => _dbContext.Contacts.AsNoTracking().AnyAsync(entity => entity.ContactTypeId == id, cancellationToken),
            LookupTable.ManagementCompanyRole => _dbContext.ManagementCompanyUsers.AsNoTracking().AnyAsync(entity => entity.ManagementCompanyRoleId == id, cancellationToken),
            _ => Task.FromResult(true)
        };
    }

    public async Task<bool> DeleteLookupItemAsync(
        LookupTable table,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return table switch
        {
            LookupTable.PropertyType => await DeleteItemAsync<PropertyType>(id, cancellationToken),
            LookupTable.TicketCategory => await DeleteItemAsync<TicketCategory>(id, cancellationToken),
            LookupTable.TicketPriority => await DeleteItemAsync<TicketPriority>(id, cancellationToken),
            LookupTable.TicketStatus => await DeleteItemAsync<TicketStatus>(id, cancellationToken),
            LookupTable.WorkStatus => await DeleteItemAsync<WorkStatus>(id, cancellationToken),
            LookupTable.ContactType => await DeleteItemAsync<ContactType>(id, cancellationToken),
            LookupTable.ManagementCompanyRole => await DeleteItemAsync<ManagementCompanyRole>(id, cancellationToken),
            _ => false
        };
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

    private async Task<IReadOnlyList<LookupItemDalDto>> GetItemsAsync<TLookup>(CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity
    {
        return await _dbContext.Set<TLookup>()
            .AsNoTracking()
            .OrderBy(entity => entity.Code)
            .Select(entity => new LookupItemDalDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Label = entity.Label.ToString()
            })
            .ToListAsync(cancellationToken);
    }

    private Task<LookupItemDalDto?> FindItemAsync<TLookup>(Guid id, CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity
    {
        return _dbContext.Set<TLookup>()
            .AsNoTracking()
            .Where(entity => entity.Id == id)
            .Select(entity => new LookupItemDalDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Label = entity.Label.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private Task<bool> CodeExistsAsync<TLookup>(string code, Guid? exceptId, CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity
    {
        var normalized = code.Trim();
        return _dbContext.Set<TLookup>()
            .AsNoTracking()
            .AnyAsync(entity => entity.Code == normalized && (!exceptId.HasValue || entity.Id != exceptId.Value), cancellationToken);
    }

    private async Task<LookupItemDalDto> CreateItemAsync<TLookup>(string code, string label, CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity, new()
    {
        var entity = new TLookup
        {
            Code = code.Trim(),
            Label = new LangStr(label.Trim())
        };

        await _dbContext.Set<TLookup>().AddAsync(entity, cancellationToken);
        return new LookupItemDalDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Label = entity.Label.ToString()
        };
    }

    private async Task<LookupItemDalDto?> UpdateItemAsync<TLookup>(Guid id, string code, string label, CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity
    {
        var entity = await _dbContext.Set<TLookup>()
            .AsTracking()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Code = code.Trim();
        entity.Label.SetTranslation(label.Trim());
        return new LookupItemDalDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Label = entity.Label.ToString()
        };
    }

    private async Task<bool> DeleteItemAsync<TLookup>(Guid id, CancellationToken cancellationToken)
        where TLookup : BaseEntity, ILookUpEntity
    {
        var entity = await _dbContext.Set<TLookup>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Set<TLookup>().Remove(entity);
        return true;
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
