using App.BLL.DTO.Properties.Models;
using App.DTO.v1.Portal.Properties;

namespace App.DTO.v1.Mappers.Portal.Properties;

public sealed class PropertyListItemApiMapper
{
    public PropertyListItemDto Map(PropertyListItemModel model)
    {
        return new PropertyListItemDto
        {
            PropertyId = model.PropertyId,
            CustomerId = model.CustomerId,
            ManagementCompanyId = model.ManagementCompanyId,
            PropertyName = model.PropertyName,
            PropertySlug = model.PropertySlug,
            AddressLine = model.AddressLine,
            City = model.City,
            PostalCode = model.PostalCode,
            PropertyTypeId = model.PropertyTypeId,
            PropertyTypeCode = model.PropertyTypeCode,
            PropertyTypeLabel = model.PropertyTypeLabel
        };
    }
}
