using App.DAL.Contracts.Repositories;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Properties;
using App.DAL.EF.Mappers.Properties;
using App.Domain;
using Base.DAL.EF;
using Base.Domain;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories;

public class PropertyRepository :
    BaseRepository<PropertyDalDto, Property, AppDbContext>,
    IPropertyRepository
{
    private const int MaxLeaseAssignmentSearchResults = 20;

    private readonly AppDbContext _dbContext;

    public PropertyRepository(AppDbContext dbContext, PropertyDalMapper mapper)
        : base(dbContext, mapper)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PropertyListItemDalDto>> AllByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.CustomerId == customerId)
            .OrderBy(property => property.Label)
            .ThenBy(property => property.Id)
            .Select(property => new PropertyListItemDalDto
            {
                Id = property.Id,
                CustomerId = property.CustomerId,
                ManagementCompanyId = property.Customer!.ManagementCompanyId,
                Name = property.Label.ToString(),
                Slug = property.Slug,
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode,
                PropertyTypeId = property.PropertyTypeId,
                PropertyTypeCode = property.PropertyType!.Code,
                PropertyTypeLabel = property.PropertyType.Label.ToString(),
                IsActive = property.IsActive
            })
            .ToListAsync(cancellationToken);

        return properties;
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
            .Select(property => new PropertyWorkspaceDalDto
            {
                Id = property.Id,
                CustomerId = property.CustomerId,
                Name = property.Label.ToString(),
                Slug = property.Slug,
                IsActive = property.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return property;
    }

    public async Task<PropertyProfileDalDto?> FindProfileAsync(
        Guid propertyId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Where(property => property.Id == propertyId && property.CustomerId == customerId)
            .Select(property => new PropertyProfileDalDto
            {
                Id = property.Id,
                CustomerId = property.CustomerId,
                ManagementCompanyId = property.Customer!.ManagementCompanyId,
                CompanySlug = property.Customer.ManagementCompany!.Slug,
                CompanyName = property.Customer.ManagementCompany.Name,
                CustomerSlug = property.Customer.Slug,
                CustomerName = property.Customer.Name,
                Name = property.Label.ToString(),
                Slug = property.Slug,
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode,
                Notes = property.Notes == null ? null : property.Notes.ToString(),
                PropertyTypeId = property.PropertyTypeId,
                PropertyTypeCode = property.PropertyType!.Code,
                PropertyTypeLabel = property.PropertyType.Label.ToString(),
                IsActive = property.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return property;
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

    public Task<bool> ExistsInCompanyAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Properties
            .AnyAsync(
                entity => entity.Id == propertyId && entity.Customer!.ManagementCompanyId == managementCompanyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<LeasePropertySearchItemDalDto>> SearchForLeaseAssignmentAsync(
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
            .Take(MaxLeaseAssignmentSearchResults)
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
        var deleted = await _dbContext.Properties
            .Where(entity => entity.Id == propertyId
                             && entity.CustomerId == customerId
                             && entity.Customer!.ManagementCompanyId == managementCompanyId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted > 0;
    }

    public async Task<IReadOnlyList<Guid>> AllIdsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Properties
            .Where(property => property.CustomerId == customerId)
            .Select(property => property.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByIdsAsync(
        IReadOnlyCollection<Guid> propertyIds,
        CancellationToken cancellationToken = default)
    {
        if (propertyIds.Count == 0)
        {
            return;
        }

        await _dbContext.Properties
            .Where(property => propertyIds.Contains(property.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
