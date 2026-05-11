using App.BLL.DTO.Customers.Models;
using App.DTO.v1.Portal.Customers;

namespace App.DTO.v1.Mappers.Portal.Customers;

public sealed class CustomerProfileApiMapper
{
    public CustomerProfileDto Map(CustomerProfileModel model)
    {
        return new CustomerProfileDto
        {
            Id = model.Id,
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            Name = model.Name,
            Slug = model.Slug,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            BillingAddress = model.BillingAddress,
            Phone = model.Phone
        };
    }
}
