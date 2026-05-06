using App.BLL.DTO.Customers.Commands;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using WebApp.ViewModels.Management.Customers;

namespace WebApp.Mappers.Mvc.Customers;

public class CompanyCustomerMvcMapper
{
    public GetCompanyCustomersQuery ToQuery(
        string companySlug,
        Guid appUserId)
    {
        return new GetCompanyCustomersQuery
        {
            CompanySlug = companySlug,
            UserId = appUserId
        };
    }

    public CreateCustomerCommand ToCommand(
        string companySlug,
        AddManagementCustomerViewModel vm,
        Guid appUserId)
    {
        return new CreateCustomerCommand
        {
            CompanySlug = companySlug,
            UserId = appUserId,
            Name = vm.Name,
            RegistryCode = vm.RegistryCode,
            BillingEmail = vm.BillingEmail,
            BillingAddress = vm.BillingAddress,
            Phone = vm.Phone
        };
    }

    public IReadOnlyList<ManagementCustomerListItemViewModel> ToListItems(IReadOnlyList<CustomerListItemModel> models)
    {
        return models.Select(model => new ManagementCustomerListItemViewModel
        {
            CustomerId = model.CustomerId,
            CustomerSlug = model.CustomerSlug,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            BillingAddress = model.BillingAddress,
            Phone = model.Phone,
            Properties = model.Properties.Select(link => new ManagementCustomerPropertyLinkViewModel
            {
                PropertySlug = link.PropertySlug,
                PropertyName = link.PropertyName
            }).ToList()
        }).ToList();
    }

}
