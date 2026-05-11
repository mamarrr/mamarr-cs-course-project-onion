using App.BLL.DTO.Customers;
using App.DTO.v1.Portal.Customers;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Customers;

public class CustomerApiMapper :
    IBaseMapper<CustomerRequestDto, CustomerBllDto>
{
    public CustomerBllDto? Map(CustomerRequestDto? entity)
    {
        return entity is null
            ? null
            : new CustomerBllDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                BillingEmail = entity.BillingEmail,
                BillingAddress = entity.BillingAddress,
                Phone = entity.Phone
            };
    }

    public CustomerRequestDto? Map(CustomerBllDto? entity)
    {
        return entity is null
            ? null
            : new CustomerRequestDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                BillingEmail = entity.BillingEmail,
                BillingAddress = entity.BillingAddress,
                Phone = entity.Phone
            };
    }
}
