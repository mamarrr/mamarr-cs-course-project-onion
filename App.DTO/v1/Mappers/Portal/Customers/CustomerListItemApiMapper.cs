using App.BLL.DTO.Customers.Models;
using App.DTO.v1.Portal.Customers;

namespace App.DTO.v1.Mappers.Portal.Customers;

public sealed class CustomerListItemApiMapper
{
    public CustomerListItemDto Map(CustomerListItemModel model)
    {
        return new CustomerListItemDto
        {
            CustomerId = model.CustomerId,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            CustomerSlug = model.CustomerSlug,
            Name = model.Name,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            BillingAddress = model.BillingAddress,
            Phone = model.Phone
        };
    }
}
