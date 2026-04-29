using Base.DAL.Contracts;

namespace App.Contracts.DAL.Customers;

public interface ICustomerRepository : IBaseRepository<CustomerDalDto>
{
    Task<CustomerProfileDalDto?> FirstProfileByCompanyAndSlugAsync(
        string companySlug,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<CustomerProfileDalDto?> FindProfileAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> RegistryCodeExistsInCompanyAsync(
        Guid managementCompanyId,
        string registryCode,
        Guid? exceptCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<string?> FindActiveManagementCompanyRoleCodeAsync(
        Guid managementCompanyId,
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(
        CustomerUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
