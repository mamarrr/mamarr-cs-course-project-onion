using App.DAL.DTO.Vendors;

namespace App.DAL.Contracts.Repositories;

public interface IVendorRepository
{
    Task<IReadOnlyList<VendorListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        VendorListFilterDalDto filter,
        CancellationToken cancellationToken = default);

    Task<VendorDetailsDalDto?> FindDetailsAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<VendorEditDalDto?> FindForEditAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> RegistryCodeExistsAsync(
        Guid managementCompanyId,
        string registryCode,
        Guid? exceptVendorId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorOptionDalDto>> TicketCategoryOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorOptionDalDto>> ContactTypeOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorOptionDalDto>> WorkStatusOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorTicketForAssignmentDalDto>> CompatibleTicketOptionsAsync(
        Guid managementCompanyId,
        Guid vendorId,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Guid> AddAsync(
        VendorCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        VendorUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> AddOrReactivateCategoryAsync(
        Guid managementCompanyId,
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> AddContactAsync(
        VendorContactCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> AssignTicketAsync(
        Guid managementCompanyId,
        Guid vendorId,
        Guid ticketId,
        Guid? assignedStatusId,
        string createdStatusCode,
        CancellationToken cancellationToken = default);

    Task<bool> AddScheduledWorkAsync(
        VendorScheduledWorkCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> VendorHasActiveCategoryAsync(
        Guid managementCompanyId,
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> TicketCategoryExistsAsync(
        Guid ticketCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ContactTypeExistsAsync(
        Guid contactTypeId,
        CancellationToken cancellationToken = default);

    Task<VendorTicketForAssignmentDalDto?> FindCompatibleTicketAsync(
        Guid managementCompanyId,
        Guid vendorId,
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<bool> WorkStatusExistsAsync(
        Guid workStatusId,
        CancellationToken cancellationToken = default);
}
