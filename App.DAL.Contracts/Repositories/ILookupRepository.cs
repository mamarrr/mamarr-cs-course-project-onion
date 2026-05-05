using App.DAL.DTO.Lookups;
using App.DAL.DTO.Leases;
using App.DAL.DTO.Properties;
using App.DAL.DTO.Tickets;

namespace App.DAL.Contracts.Repositories;

public interface ILookupRepository
{
    Task<LookupDalDto?> FindManagementCompanyJoinRequestStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyJoinRequestStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindManagementCompanyRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindManagementCompanyRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LookupDalDto>> AllManagementCompanyRolesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindCustomerRepresentativeRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindLeaseRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> LeaseRoleExistsAsync(
        Guid leaseRoleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaseRoleOptionDalDto>> ListLeaseRolesAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindPropertyTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> PropertyTypeExistsAsync(
        Guid propertyTypeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PropertyTypeOptionDalDto>> AllPropertyTypeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<LookupDalDto?> FindContactTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<TicketOptionDalDto?> FindTicketStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<TicketOptionDalDto?> FindTicketStatusByIdAsync(
        Guid statusId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> AllTicketStatusesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> AllTicketPrioritiesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> AllTicketCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> TicketCategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> TicketPriorityExistsAsync(
        Guid priorityId,
        CancellationToken cancellationToken = default);

    Task<bool> TicketStatusExistsAsync(
        Guid statusId,
        CancellationToken cancellationToken = default);
}
