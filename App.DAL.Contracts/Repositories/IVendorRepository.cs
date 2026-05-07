using App.DAL.DTO.Tickets;
using App.DAL.DTO.Vendors;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IVendorRepository : IBaseRepository<VendorDalDto>
{
    Task<IReadOnlyList<VendorListItemDalDto>> AllByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<VendorProfileDalDto?> FindProfileAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> RegistryCodeExistsInCompanyAsync(
        Guid managementCompanyId,
        string registryCode,
        Guid? exceptVendorId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasDeleteDependenciesAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
}
