using App.DAL.DTO.Vendors;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IVendorTicketCategoryRepository : IBaseRepository<VendorTicketCategoryDalDto>
{
    Task<IReadOnlyList<VendorCategoryAssignmentDalDto>> AllByVendorAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<VendorTicketCategoryDalDto?> FindInCompanyAsync(
        Guid vendorTicketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<VendorTicketCategoryDalDto?> FindByVendorCategoryInCompanyAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid vendorTicketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAssignmentAsync(
        Guid vendorId,
        Guid ticketCategoryId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}

