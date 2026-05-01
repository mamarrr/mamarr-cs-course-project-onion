using App.Contracts.DAL.Customers;
using App.Domain;
using Base.Contracts;

namespace App.DAL.EF.Mappers.Customers;

public sealed class CustomerDalMapper : IMapper<CustomerDalDto, Customer>
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
