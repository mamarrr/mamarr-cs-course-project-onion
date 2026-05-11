using App.BLL.DTO.Properties.Models;
using App.DTO.v1.Portal.Properties;

namespace App.DTO.v1.Mappers.Portal.Properties;

public class PropertyProfileApiMapper
{
    public PropertyProfileDto Map(PropertyProfileModel model)
    {
        return new PropertyProfileDto
        {
            PropertyId = model.PropertyId,
            CustomerId = model.CustomerId,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            PropertySlug = model.PropertySlug,
            Name = model.Name,
            AddressLine = model.AddressLine,
            City = model.City,
            PostalCode = model.PostalCode,
            Notes = model.Notes,
            PropertyTypeId = model.PropertyTypeId,
            PropertyTypeCode = model.PropertyTypeCode,
            PropertyTypeLabel = model.PropertyTypeLabel
        };
    }
}
