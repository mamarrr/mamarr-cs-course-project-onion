using App.DAL.DTO.Contacts;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IContactRepository : IBaseRepository<ContactDalDto>
{
    Task<ContactDalDto?> FindInCompanyAsync(
        Guid contactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid contactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContactDalDto>> OptionsByCompanyAsync(
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DuplicateValueExistsAsync(
        Guid managementCompanyId,
        Guid contactTypeId,
        string contactValue,
        Guid? exceptContactId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasDependenciesAsync(
        Guid contactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
