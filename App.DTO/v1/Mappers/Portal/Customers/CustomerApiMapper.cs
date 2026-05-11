using App.BLL.DTO.Customers;
using App.DTO.v1.Portal.Customers;
using Base.Contracts;

namespace App.DTO.v1.Mappers.Portal.Customers;

public sealed class CustomerApiMapper :
    IBaseMapper<CreateCustomerDto, CustomerBllDto>,
    IBaseMapper<UpdateCustomerProfileDto, CustomerBllDto>
{
    public CustomerBllDto? Map(CreateCustomerDto? entity)
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

    public CustomerBllDto? Map(UpdateCustomerProfileDto? entity)
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

    CreateCustomerDto? IBaseMapper<CreateCustomerDto, CustomerBllDto>.Map(CustomerBllDto? entity)
    {
        return entity is null
            ? null
            : new CreateCustomerDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                BillingEmail = entity.BillingEmail,
                BillingAddress = entity.BillingAddress,
                Phone = entity.Phone
            };
    }

    UpdateCustomerProfileDto? IBaseMapper<UpdateCustomerProfileDto, CustomerBllDto>.Map(CustomerBllDto? entity)
    {
        return entity is null
            ? null
            : new UpdateCustomerProfileDto
            {
                Name = entity.Name,
                RegistryCode = entity.RegistryCode,
                BillingEmail = entity.BillingEmail,
                BillingAddress = entity.BillingAddress,
                Phone = entity.Phone
            };
    }
}
