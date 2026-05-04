using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Properties.Models;
using App.DAL.Contracts.DAL.Properties;

namespace App.BLL.Mappers.Properties;

public static class PropertyBllMapper
{
    public static PropertyWorkspaceModel MapWorkspace(
        CustomerWorkspaceModel customer,
        PropertyWorkspaceDalDto property)
    {
        return new PropertyWorkspaceModel
        {
            AppUserId = customer.AppUserId,
            ManagementCompanyId = customer.ManagementCompanyId,
            CompanySlug = customer.CompanySlug,
            CompanyName = customer.CompanyName,
            CustomerId = customer.CustomerId,
            CustomerSlug = customer.CustomerSlug,
            CustomerName = customer.CustomerName,
            PropertyId = property.Id,
            PropertySlug = property.Slug,
            PropertyName = property.Name
        };
    }

    public static PropertyListItemModel MapListItem(PropertyListItemDalDto property)
    {
        return new PropertyListItemModel
        {
            PropertyId = property.Id,
            CustomerId = property.CustomerId,
            ManagementCompanyId = property.ManagementCompanyId,
            PropertyName = property.Name,
            PropertySlug = property.Slug,
            AddressLine = property.AddressLine,
            City = property.City,
            PostalCode = property.PostalCode,
            PropertyTypeId = property.PropertyTypeId,
            PropertyTypeCode = property.PropertyTypeCode,
            PropertyTypeLabel = property.PropertyTypeLabel,
            IsActive = property.IsActive
        };
    }

    public static PropertyProfileModel MapProfile(PropertyProfileDalDto property)
    {
        return new PropertyProfileModel
        {
            PropertyId = property.Id,
            CustomerId = property.CustomerId,
            ManagementCompanyId = property.ManagementCompanyId,
            CompanySlug = property.CompanySlug,
            CompanyName = property.CompanyName,
            CustomerSlug = property.CustomerSlug,
            CustomerName = property.CustomerName,
            PropertySlug = property.Slug,
            Name = property.Name,
            AddressLine = property.AddressLine,
            City = property.City,
            PostalCode = property.PostalCode,
            Notes = property.Notes,
            PropertyTypeId = property.PropertyTypeId,
            PropertyTypeCode = property.PropertyTypeCode,
            PropertyTypeLabel = property.PropertyTypeLabel,
            IsActive = property.IsActive
        };
    }

    public static PropertyTypeOptionModel MapTypeOption(PropertyTypeOptionDalDto option)
    {
        return new PropertyTypeOptionModel
        {
            Id = option.Id,
            Code = option.Code,
            Label = option.Label
        };
    }
}
