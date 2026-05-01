using App.Contracts.DAL.Properties;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Properties;

public class PropertyDalMapper : IMapper<PropertyDalDto, Property>
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
