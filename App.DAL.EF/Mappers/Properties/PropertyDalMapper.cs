using App.Contracts.DAL.Properties;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Properties;

public sealed class PropertyDalMapper : IMapper<PropertyDalDto, Property>
{
    public PropertyDalDto? Map(Property? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new PropertyDalDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            ManagementCompanyId = entity.Customer?.ManagementCompanyId ?? Guid.Empty,
            Name = entity.Label.ToString(),
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }

    public PropertyListItemDalDto MapListItem(Property entity)
    {
        return new PropertyListItemDalDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            ManagementCompanyId = entity.Customer!.ManagementCompanyId,
            Name = entity.Label.ToString(),
            Slug = entity.Slug,
            AddressLine = entity.AddressLine,
            City = entity.City,
            PostalCode = entity.PostalCode,
            PropertyTypeId = entity.PropertyTypeId,
            PropertyTypeCode = entity.PropertyType!.Code,
            PropertyTypeLabel = entity.PropertyType.Label.ToString(),
            IsActive = entity.IsActive
        };
    }

    public PropertyWorkspaceDalDto MapWorkspace(Property entity)
    {
        return new PropertyWorkspaceDalDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            Name = entity.Label.ToString(),
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }

    public PropertyProfileDalDto MapProfile(Property entity)
    {
        return new PropertyProfileDalDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            ManagementCompanyId = entity.Customer!.ManagementCompanyId,
            CompanySlug = entity.Customer.ManagementCompany!.Slug,
            CompanyName = entity.Customer.ManagementCompany.Name,
            CustomerSlug = entity.Customer.Slug,
            CustomerName = entity.Customer.Name,
            Name = entity.Label.ToString(),
            Slug = entity.Slug,
            AddressLine = entity.AddressLine,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Notes = entity.Notes?.ToString(),
            PropertyTypeId = entity.PropertyTypeId,
            PropertyTypeCode = entity.PropertyType!.Code,
            PropertyTypeLabel = entity.PropertyType.Label.ToString(),
            IsActive = entity.IsActive
        };
    }

    public Property? Map(PropertyDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Property
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            Label = entity.Name,
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }
}
