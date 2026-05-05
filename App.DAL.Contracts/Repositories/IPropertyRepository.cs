using App.DAL.DTO.Leases;
using App.DAL.DTO.Properties;
using App.DAL.DTO.Tickets;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IPropertyRepository : IBaseRepository<PropertyDalDto>
{
    Task<IReadOnlyList<PropertyListItemDalDto>> AllByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<PropertyWorkspaceDalDto?> FirstWorkspaceByCustomerAndSlugAsync(
        Guid customerId,
        string propertySlug,
        CancellationToken cancellationToken = default);

    Task<PropertyProfileDalDto?> FindProfileAsync(
        Guid propertyId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> SlugExistsForCustomerAsync(
        Guid customerId,
        string slug,
        Guid? exceptPropertyId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCustomerAsync(
        Guid propertyId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? customerId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeasePropertySearchItemDalDto>> SearchForLeaseAssignmentAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<bool> HasDeleteDependenciesAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
