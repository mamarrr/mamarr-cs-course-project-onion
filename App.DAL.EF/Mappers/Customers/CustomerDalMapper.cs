using App.DAL.DTO.Customers;
using App.Domain;
using Base.Contracts;
using Base.Domain;

namespace App.DAL.EF.Mappers.Customers;

public class CustomerDalMapper : IBaseMapper<CustomerDalDto, Customer>
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
            BillingEmail = entity.BillingEmail,
            BillingAddress = entity.BillingAddress,
            Phone = entity.Phone,
            Notes = entity.Notes?.ToString(),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
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
            BillingEmail = entity.BillingEmail,
            BillingAddress = entity.BillingAddress,
            Phone = entity.Phone,
            Notes = string.IsNullOrWhiteSpace(entity.Notes) ? null : new LangStr(entity.Notes.Trim()),
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }
}
