using App.DAL.DTO.Leases;
using App.DAL.DTO.Properties;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IPropertyRepository : IBaseRepository<PropertyDalDto>
{
    Task<IReadOnlyList<PropertyListItemDalDto>> AllByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyTypeOptionDalDto>> AllPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<PropertyWorkspaceDalDto?> FirstWorkspaceByCustomerAndSlugAsync(
        Guid customerId,
        string propertySlug,
        CancellationToken cancellationToken = default);

    Task<PropertyProfileDalDto?> FindProfileAsync(
        Guid propertyId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> PropertyTypeExistsAsync(
        Guid propertyTypeId,
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

    Task<IReadOnlyList<LeasePropertySearchItemDalDto>> SearchForLeaseAssignmentAsync(
        Guid managementCompanyId,
        string? searchTerm,
        CancellationToken cancellationToken = default);

    Task<PropertyDalDto> AddAsync(
        PropertyCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateProfileAsync(
        PropertyUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> AllIdsByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task DeleteByIdsAsync(
        IReadOnlyCollection<Guid> propertyIds,
        CancellationToken cancellationToken = default);
}
