using App.BLL.Contracts.Customers.Models;
using App.DAL.DTO.Customers;

namespace App.BLL.Mappers.Customers;

public static class CustomerProfileBllMapper
{
    public static CustomerProfileModel Map(CustomerProfileDalDto dto)
    {
        return new CustomerProfileModel
        {
            Id = dto.Id,
            ManagementCompanyId = dto.ManagementCompanyId,
            CompanySlug = dto.CompanySlug,
            CompanyName = dto.CompanyName,
            Name = dto.Name,
            Slug = dto.Slug,
            RegistryCode = dto.RegistryCode,
            BillingEmail = dto.BillingEmail,
            BillingAddress = dto.BillingAddress,
            Phone = dto.Phone,
            IsActive = dto.IsActive
        };
    }
}
