using System.Security.Claims;
using App.BLL.Contracts.Customers.Commands;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using WebApp.ViewModels.Management.Customers;

namespace WebApp.Mappers.Mvc.Customers;

public class CompanyCustomerMvcMapper
{
    public GetCompanyCustomersQuery ToQuery(
        string companySlug,
        ClaimsPrincipal user)
    {
        return new GetCompanyCustomersQuery
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user)
        };
    }

    public CreateCustomerCommand ToCommand(
        string companySlug,
        AddManagementCustomerViewModel vm,
        ClaimsPrincipal user)
    {
        return new CreateCustomerCommand
        {
            CompanySlug = companySlug,
            UserId = GetAppUserId(user),
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

    private static Guid GetAppUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var appUserId) ? appUserId : Guid.Empty;
    }
}
