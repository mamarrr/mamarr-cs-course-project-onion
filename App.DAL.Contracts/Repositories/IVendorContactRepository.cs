using App.DAL.DTO.Vendors;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IVendorContactRepository : IBaseRepository<VendorContactDalDto>
{
    Task<IReadOnlyList<VendorContactAssignmentDalDto>> AllByVendorAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<VendorContactDalDto?> FindInCompanyAsync(
        Guid vendorContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid vendorContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPrimaryAsync(
        Guid vendorId,
        Guid managementCompanyId,
        Guid? exceptVendorContactId = null,
        CancellationToken cancellationToken = default);

    Task ClearPrimaryAsync(
        Guid vendorId,
        Guid managementCompanyId,
        Guid? exceptVendorContactId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ContactLinkedToVendorAsync(
        Guid vendorId,
        Guid contactId,
        Guid managementCompanyId,
        Guid? exceptVendorContactId = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteInCompanyAsync(
        Guid vendorContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
