using App.DAL.DTO.Customers;
using App.DAL.DTO.Tickets;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface ICustomerRepository : IBaseRepository<CustomerDalDto>
{
    Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanySlugAsync(
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerListItemDalDto>> AllByCompanyIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerPropertyLinkDalDto>> AllPropertyLinksByCompanyIdAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> CustomerSlugExistsInCompanyAsync(
        Guid managementCompanyId,
        string slug,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<CustomerWorkspaceDalDto?> FirstWorkspaceByCompanyAndSlugAsync(
        Guid managementCompanyId,
        string customerSlug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerUserContextDalDto>> ActiveUserCustomerContextsAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveUserCustomerContextExistsAsync(
        Guid appUserId,
        Guid customerId,
        CancellationToken cancellationToken = default);

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

    Task<bool> HasDeleteDependenciesAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
