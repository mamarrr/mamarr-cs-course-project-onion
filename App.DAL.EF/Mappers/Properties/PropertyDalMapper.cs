using App.DAL.DTO.Properties;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Properties;

public class PropertyDalMapper : IBaseMapper<PropertyDalDto, Property>
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
