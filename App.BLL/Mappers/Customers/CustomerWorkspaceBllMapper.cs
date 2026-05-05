using App.BLL.Contracts.Customers.Models;
using App.DAL.DTO.Customers;
using App.DAL.DTO.ManagementCompanies;

namespace App.BLL.Mappers.Customers;

public static class CustomerWorkspaceBllMapper
{
    public static CompanyWorkspaceModel MapCompany(ManagementCompanyDalDto company, Guid appUserId)
    {
        return new CompanyWorkspaceModel
        {
            AppUserId = appUserId,
            ManagementCompanyId = company.Id,
            CompanySlug = company.Slug,
            CompanyName = company.Name
        };
    }

    public static CustomerWorkspaceModel MapWorkspace(CustomerWorkspaceDalDto customer, Guid appUserId)
    {
        return new CustomerWorkspaceModel
        {
            AppUserId = appUserId,
            ManagementCompanyId = customer.ManagementCompanyId,
            CompanySlug = customer.CompanySlug,
            CompanyName = customer.CompanyName,
            CustomerId = customer.Id,
            CustomerSlug = customer.Slug,
            CustomerName = customer.Name
        };
    }

    public static CustomerListItemModel MapListItem(
        CustomerListItemDalDto customer,
        CompanyWorkspaceModel company,
        IReadOnlyList<CustomerPropertyLinkModel> propertyLinks)
    {
        return new CustomerListItemModel
        {
            CustomerId = customer.Id,
            ManagementCompanyId = customer.ManagementCompanyId,
            CompanySlug = company.CompanySlug,
            CompanyName = company.CompanyName,
            CustomerSlug = customer.Slug,
            Name = customer.Name,
            RegistryCode = customer.RegistryCode,
            BillingEmail = customer.BillingEmail,
            BillingAddress = customer.BillingAddress,
            Phone = customer.Phone,
            Properties = propertyLinks
        };
    }

    public static CompanyCustomerModel MapCreated(
        CustomerDalDto customer,
        CompanyWorkspaceModel company,
        CustomerCreateDalDto source)
    {
        return new CompanyCustomerModel
        {
            CustomerId = customer.Id,
            ManagementCompanyId = customer.ManagementCompanyId,
            CompanySlug = company.CompanySlug,
            CompanyName = company.CompanyName,
            CustomerSlug = customer.Slug,
            Name = customer.Name,
            RegistryCode = customer.RegistryCode,
            BillingEmail = source.BillingEmail,
            BillingAddress = source.BillingAddress,
            Phone = source.Phone,
        };
    }
}
