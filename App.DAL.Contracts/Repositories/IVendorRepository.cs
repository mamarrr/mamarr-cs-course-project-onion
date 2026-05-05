using App.DAL.DTO.Tickets;
using App.DAL.DTO.Vendors;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IVendorRepository : IBaseRepository<VendorDalDto>
{
    Task<bool> ExistsInCompanyAsync(
        Guid vendorId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketOptionDalDto>> OptionsForTicketAsync(
        Guid managementCompanyId,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
}
