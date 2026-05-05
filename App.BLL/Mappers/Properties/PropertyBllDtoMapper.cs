using App.BLL.Contracts.Properties;
using App.DAL.DTO.Properties;
using Base.Contracts;

namespace App.BLL.Mappers.Properties;

public class PropertyBllDtoMapper : IBaseMapper<PropertyBllDto, PropertyDalDto>
{
    public PropertyBllDto? Map(PropertyDalDto? entity)
    {
        if (entity is null) return null;

        return new PropertyBllDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            PropertyTypeId = entity.PropertyTypeId,
            Label = entity.Label,
            Slug = entity.Slug,
            AddressLine = entity.AddressLine,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Notes = entity.Notes
        };
    }

    public PropertyDalDto? Map(PropertyBllDto? entity)
    {
        if (entity is null) return null;

        return new PropertyDalDto
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            PropertyTypeId = entity.PropertyTypeId,
            Label = entity.Label,
            Slug = entity.Slug,
            AddressLine = entity.AddressLine,
            City = entity.City,
            PostalCode = entity.PostalCode,
            Notes = entity.Notes
        };
    }
}

