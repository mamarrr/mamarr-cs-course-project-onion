using App.Contracts.DAL.Customers;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Customers;

public sealed class CustomerDalMapper : IBaseMapper<CustomerDalDto, Customer>
{
    public CustomerDalDto? Map(Customer? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new CustomerDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            IsActive = entity.IsActive
        };
    }

    public CustomerListItemDalDto MapListItem(Customer entity)
    {
        return new CustomerListItemDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            BillingEmail = entity.BillingEmail,
            BillingAddress = entity.BillingAddress,
            Phone = entity.Phone,
            IsActive = entity.IsActive
        };
    }

    public CustomerPropertyLinkDalDto MapPropertyLink(Property entity)
    {
        return new CustomerPropertyLinkDalDto
        {
            CustomerId = entity.CustomerId,
            PropertySlug = entity.Slug,
            PropertyName = entity.Label.ToString()
        };
    }

    public CustomerWorkspaceDalDto MapWorkspace(Customer entity)
    {
        return new CustomerWorkspaceDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            CompanySlug = entity.ManagementCompany!.Slug,
            CompanyName = entity.ManagementCompany.Name,
            Name = entity.Name,
            Slug = entity.Slug,
            IsActive = entity.IsActive
        };
    }

    public CustomerProfileDalDto MapProfile(Customer entity)
    {
        return new CustomerProfileDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            CompanySlug = entity.ManagementCompany!.Slug,
            CompanyName = entity.ManagementCompany.Name,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            BillingEmail = entity.BillingEmail,
            BillingAddress = entity.BillingAddress,
            Phone = entity.Phone,
            IsActive = entity.IsActive
        };
    }

    public Customer? Map(CustomerDalDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new Customer
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            IsActive = entity.IsActive
        };
    }
}
