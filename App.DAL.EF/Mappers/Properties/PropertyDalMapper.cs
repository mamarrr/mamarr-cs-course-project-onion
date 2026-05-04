using App.DAL.DTO.Properties;
using App.Domain;
using Base.Contracts;
using Base.Domain;

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
            PropertyTypeId = entity.PropertyTypeId,
            Label = entity.Label.ToString(),
            Slug = entity.Slug,
            AddressLine = entity.AddressLine,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Notes = entity.Notes?.ToString(),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
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
            PropertyTypeId = entity.PropertyTypeId,
            Label = new LangStr(entity.Label.Trim()),
            Slug = entity.Slug,
            AddressLine = entity.AddressLine,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim()),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }
}
