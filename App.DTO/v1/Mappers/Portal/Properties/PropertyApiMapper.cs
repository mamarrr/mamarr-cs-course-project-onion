using App.BLL.DTO.Properties;
using App.DTO.v1.Portal.Properties;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Properties;

public sealed class PropertyApiMapper :
    IBaseMapper<CreatePropertyDto, PropertyBllDto>,
    IBaseMapper<UpdatePropertyProfileDto, PropertyBllDto>
{
    public PropertyBllDto? Map(CreatePropertyDto? entity)
    {
        return entity is null
            ? null
            : new PropertyBllDto
            {
                Label = entity.Name,
                PropertyTypeId = entity.PropertyTypeId,
                AddressLine = entity.AddressLine,
                City = entity.City,
                PostalCode = entity.PostalCode,
                Notes = entity.Notes
            };
    }

    public PropertyBllDto? Map(UpdatePropertyProfileDto? entity)
    {
        return entity is null
            ? null
            : new PropertyBllDto
            {
                Label = entity.Name,
                AddressLine = entity.AddressLine,
                City = entity.City,
                PostalCode = entity.PostalCode,
                Notes = entity.Notes
            };
    }

    CreatePropertyDto? IBaseMapper<CreatePropertyDto, PropertyBllDto>.Map(PropertyBllDto? entity)
    {
        return entity is null
            ? null
            : new CreatePropertyDto
            {
                Name = entity.Label,
                PropertyTypeId = entity.PropertyTypeId,
                AddressLine = entity.AddressLine,
                City = entity.City,
                PostalCode = entity.PostalCode,
                Notes = entity.Notes
            };
    }

    UpdatePropertyProfileDto? IBaseMapper<UpdatePropertyProfileDto, PropertyBllDto>.Map(PropertyBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdatePropertyProfileDto
            {
                Name = entity.Label,
                AddressLine = entity.AddressLine,
                City = entity.City,
                PostalCode = entity.PostalCode,
                Notes = entity.Notes
            };
    }
}
