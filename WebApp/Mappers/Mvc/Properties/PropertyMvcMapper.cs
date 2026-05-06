using App.BLL.DTO.Properties.Commands;
using App.BLL.DTO.Properties.Models;
using App.BLL.DTO.Properties.Queries;
using WebApp.ViewModels.Customer.CustomerProperties;
using WebApp.ViewModels.Property;

namespace WebApp.Mappers.Mvc.Properties;

public class PropertyMvcMapper
{
    public GetPropertyWorkspaceQuery ToWorkspaceQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        Guid appUserId)
    {
        return new GetPropertyWorkspaceQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = appUserId
        };
    }

    public GetPropertyWorkspaceQuery ToCustomerPropertiesQuery(
        string companySlug,
        string customerSlug,
        Guid appUserId)
    {
        return new GetPropertyWorkspaceQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = appUserId
        };
    }

    public GetPropertyProfileQuery ToProfileQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        Guid appUserId)
    {
        return new GetPropertyProfileQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = appUserId
        };
    }

    public CreatePropertyCommand ToCreateCommand(
        string companySlug,
        string customerSlug,
        AddPropertyViewModel vm,
        Guid appUserId)
    {
        return new CreatePropertyCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = appUserId,
            Name = vm.Name,
            AddressLine = vm.AddressLine,
            City = vm.City,
            PostalCode = vm.PostalCode,
            PropertyTypeId = vm.PropertyTypeId ?? Guid.Empty,
            Notes = vm.Notes,
        };
    }

    public UpdatePropertyProfileCommand ToUpdateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        PropertyProfileEditViewModel edit,
        Guid appUserId)
    {
        return new UpdatePropertyProfileCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = appUserId,
            Name = edit.Name,
            AddressLine = edit.AddressLine,
            City = edit.City,
            PostalCode = edit.PostalCode,
            Notes = edit.Notes,
        };
    }

    public DeletePropertyCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        PropertyProfileEditViewModel edit,
        Guid appUserId)
    {
        return new DeletePropertyCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = appUserId,
            ConfirmationName = edit.DeleteConfirmation ?? string.Empty
        };
    }

    public IReadOnlyList<CustomerPropertyListItemViewModel> ToCustomerPropertyListItems(
        IReadOnlyList<PropertyListItemModel> properties)
    {
        return properties.Select(property => new CustomerPropertyListItemViewModel
        {
            PropertyId = property.PropertyId,
            PropertySlug = property.PropertySlug,
            PropertyName = property.PropertyName,
            AddressLine = property.AddressLine,
            City = property.City,
            PostalCode = property.PostalCode,
            PropertyTypeLabel = property.PropertyTypeLabel,
        }).ToList();
    }

    public IReadOnlyList<PropertyTypeOptionViewModel> ToPropertyTypeOptions(
        IReadOnlyList<PropertyTypeOptionModel> propertyTypes)
    {
        return propertyTypes.Select(propertyType => new PropertyTypeOptionViewModel
        {
            Id = propertyType.Id,
            Label = propertyType.Label
        }).ToList();
    }

    public PropertyProfileEditViewModel ToEditViewModel(PropertyProfileModel profile)
    {
        return new PropertyProfileEditViewModel
        {
            Name = profile.Name,
            AddressLine = profile.AddressLine,
            City = profile.City,
            PostalCode = profile.PostalCode,
            Notes = profile.Notes,
        };
    }

}
