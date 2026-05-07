using App.DAL.DTO.Residents;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IResidentContactRepository : IBaseRepository<ResidentContactDalDto>
{
    Task<IReadOnlyList<ResidentContactAssignmentDalDto>> AllByResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<ResidentContactDalDto?> FindInCompanyAsync(
        Guid residentContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInCompanyAsync(
        Guid residentContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPrimaryAsync(
        Guid residentId,
        Guid managementCompanyId,
        Guid? exceptResidentContactId = null,
        CancellationToken cancellationToken = default);

    Task ClearPrimaryAsync(
        Guid residentId,
        Guid managementCompanyId,
        Guid? exceptResidentContactId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ContactLinkedToResidentAsync(
        Guid residentId,
        Guid contactId,
        Guid managementCompanyId,
        Guid? exceptResidentContactId = null,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteInCompanyAsync(
        Guid residentContactId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
