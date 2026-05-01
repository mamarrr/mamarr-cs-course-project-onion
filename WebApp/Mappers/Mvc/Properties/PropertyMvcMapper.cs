using System.Security.Claims;
using App.BLL.Contracts.Properties.Commands;
using App.BLL.Contracts.Properties.Models;
using App.BLL.Contracts.Properties.Queries;
using WebApp.ViewModels.Customer.CustomerProperties;
using WebApp.ViewModels.Property;

namespace WebApp.Mappers.Mvc.Properties;

public class PropertyMvcMapper
{
    public GetPropertyWorkspaceQuery ToWorkspaceQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        ClaimsPrincipal user)
    {
        return new GetPropertyWorkspaceQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user)
        };
    }

    public GetPropertyWorkspaceQuery ToCustomerPropertiesQuery(
        string companySlug,
        string customerSlug,
        ClaimsPrincipal user)
    {
        return new GetPropertyWorkspaceQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user)
        };
    }

    public GetPropertyProfileQuery ToProfileQuery(
        string companySlug,
        string customerSlug,
        string propertySlug,
        ClaimsPrincipal user)
    {
        return new GetPropertyProfileQuery
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user)
        };
    }

    public CreatePropertyCommand ToCreateCommand(
        string companySlug,
        string customerSlug,
        AddPropertyViewModel vm,
        ClaimsPrincipal user)
    {
        return new CreatePropertyCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user),
            Name = vm.Name,
            AddressLine = vm.AddressLine,
            City = vm.City,
            PostalCode = vm.PostalCode,
            PropertyTypeId = vm.PropertyTypeId ?? Guid.Empty,
            Notes = vm.Notes,
            IsActive = vm.IsActive
        };
    }

    public UpdatePropertyProfileCommand ToUpdateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        PropertyProfileEditViewModel edit,
        ClaimsPrincipal user)
    {
        return new UpdatePropertyProfileCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user),
            Name = edit.Name,
            AddressLine = edit.AddressLine,
            City = edit.City,
            PostalCode = edit.PostalCode,
            Notes = edit.Notes,
            IsActive = edit.IsActive
        };
    }

    public DeletePropertyCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        PropertyProfileEditViewModel edit,
        ClaimsPrincipal user)
    {
        return new DeletePropertyCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user),
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
            IsActive = property.IsActive
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
            IsActive = profile.IsActive
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
