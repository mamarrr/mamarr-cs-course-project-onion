using System.Security.Claims;
using App.BLL.Contracts.Properties.Commands;
using App.BLL.Contracts.Properties.Models;
using App.BLL.Contracts.Properties.Queries;
using App.DTO.v1.Customer;
using App.DTO.v1.Property;
using App.DTO.v1.Shared;

namespace WebApp.Mappers.Api.Properties;

public class PropertyApiMapper
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
        CreateCustomerPropertyRequestDto dto,
        ClaimsPrincipal user)
    {
        return new CreatePropertyCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            UserId = GetAppUserId(user),
            Name = dto.Name,
            AddressLine = dto.AddressLine,
            City = dto.City,
            PostalCode = dto.PostalCode,
            PropertyTypeId = dto.PropertyTypeId ?? Guid.Empty,
            Notes = dto.Notes,
            IsActive = dto.IsActive
        };
    }

    public UpdatePropertyProfileCommand ToUpdateCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        UpdatePropertyProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new UpdatePropertyProfileCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user),
            Name = dto.Name,
            AddressLine = dto.AddressLine,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Notes = dto.Notes,
            IsActive = dto.IsActive
        };
    }

    public DeletePropertyCommand ToDeleteCommand(
        string companySlug,
        string customerSlug,
        string propertySlug,
        DeletePropertyProfileRequestDto dto,
        ClaimsPrincipal user)
    {
        return new DeletePropertyCommand
        {
            CompanySlug = companySlug,
            CustomerSlug = customerSlug,
            PropertySlug = propertySlug,
            UserId = GetAppUserId(user),
            ConfirmationName = dto.ConfirmationName
        };
    }

    public CustomerPropertiesResponseDto ToCustomerPropertiesResponseDto(
        IReadOnlyList<PropertyListItemModel> properties,
        IReadOnlyList<PropertyTypeOptionModel> propertyTypeOptions,
        string companySlug,
        string companyName,
        string customerSlug,
        string customerName)
    {
        return new CustomerPropertiesResponseDto
        {
            Properties = properties.Select(property => new CustomerPropertySummaryDto
            {
                PropertyId = property.PropertyId,
                PropertySlug = property.PropertySlug,
                PropertyName = property.PropertyName,
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode,
                PropertyTypeId = property.PropertyTypeId,
                PropertyTypeCode = property.PropertyTypeCode,
                PropertyTypeLabel = property.PropertyTypeLabel,
                IsActive = property.IsActive,
                RouteContext = CreatePropertyRouteContext(
                    companySlug,
                    companyName,
                    customerSlug,
                    customerName,
                    property.PropertySlug,
                    property.PropertyName,
                    "property-dashboard")
            }).ToList(),
            PropertyTypeOptions = propertyTypeOptions.Select(ToLookupOptionDto).ToList()
        };
    }

    public PropertyDashboardResponseDto ToDashboardResponseDto(PropertyDashboardModel model)
    {
        return new PropertyDashboardResponseDto
        {
            Dashboard = new ApiDashboardDto
            {
                RouteContext = CreatePropertyRouteContext(
                    model.Workspace.CompanySlug,
                    model.Workspace.CompanyName,
                    model.Workspace.CustomerSlug,
                    model.Workspace.CustomerName,
                    model.Workspace.PropertySlug,
                    model.Workspace.PropertyName,
                    "property-dashboard"),
                Title = model.Title,
                SectionLabel = model.SectionLabel,
                Widgets = model.Widgets
            }
        };
    }

    public PropertyProfileResponseDto ToProfileResponseDto(PropertyProfileModel model)
    {
        return new PropertyProfileResponseDto
        {
            Profile = ToProfileDto(model)
        };
    }

    public CreateCustomerPropertyResponseDto ToCreateResponseDto(PropertyProfileModel model)
    {
        return new CreateCustomerPropertyResponseDto
        {
            PropertyId = model.PropertyId,
            PropertySlug = model.PropertySlug,
            RouteContext = CreatePropertyRouteContext(
                model.CompanySlug,
                model.CompanyName,
                model.CustomerSlug,
                model.CustomerName,
                model.PropertySlug,
                model.Name,
                "property-dashboard")
        };
    }

    public PropertyProfileDto ToProfileDto(PropertyProfileModel model)
    {
        return new PropertyProfileDto
        {
            PropertyId = model.PropertyId,
            PropertySlug = model.PropertySlug,
            Name = model.Name,
            AddressLine = model.AddressLine,
            City = model.City,
            PostalCode = model.PostalCode,
            Notes = model.Notes,
            IsActive = model.IsActive,
            RouteContext = CreatePropertyRouteContext(
                model.CompanySlug,
                model.CompanyName,
                model.CustomerSlug,
                model.CustomerName,
                model.PropertySlug,
                model.Name,
                "property-profile")
        };
    }

    private static LookupOptionDto ToLookupOptionDto(PropertyTypeOptionModel model)
    {
        return new LookupOptionDto
        {
            Id = model.Id,
            Code = model.Code,
            Label = model.Label
        };
    }

    private static ApiRouteContextDto CreatePropertyRouteContext(
        string companySlug,
        string companyName,
        string customerSlug,
        string customerName,
        string propertySlug,
        string propertyName,
        string currentSection)
    {
        return new ApiRouteContextDto
        {
            CompanySlug = companySlug,
            CompanyName = companyName,
            CustomerSlug = customerSlug,
            CustomerName = customerName,
            PropertySlug = propertySlug,
            PropertyName = propertyName,
            CurrentSection = currentSection
        };
    }

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
