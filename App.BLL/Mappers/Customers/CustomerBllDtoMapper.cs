using App.BLL.Contracts.Customers;
using App.DAL.DTO.Customers;
using Base.Contracts;

namespace App.BLL.Mappers.Customers;

public class CustomerBllDtoMapper : IBaseMapper<CustomerBllDto, CustomerDalDto>
{
    public CustomerBllDto? Map(CustomerDalDto? entity)
    {
        if (entity is null) return null;

        return new CustomerBllDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            BillingEmail = entity.BillingEmail,
            BillingAddress = entity.BillingAddress,
            Phone = entity.Phone,
            Notes = entity.Notes
        };
    }

    public CustomerDalDto? Map(CustomerBllDto? entity)
    {
        if (entity is null) return null;

        return new CustomerDalDto
        {
            Id = entity.Id,
            ManagementCompanyId = entity.ManagementCompanyId,
            Name = entity.Name,
            Slug = entity.Slug,
            RegistryCode = entity.RegistryCode,
            BillingEmail = entity.BillingEmail,
            BillingAddress = entity.BillingAddress,
            Phone = entity.Phone,
            Notes = entity.Notes
        };
    }
}

