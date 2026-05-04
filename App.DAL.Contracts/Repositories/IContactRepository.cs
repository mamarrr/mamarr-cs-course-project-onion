using App.DAL.DTO.Contacts;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IContactRepository : IBaseRepository<ContactDalDto>
{
    Task<ContactDalDto?> FindAsync(
        Guid contactId,
        CancellationToken cancellationToken = default);

    Task<ContactDalDto> AddAsync(
        ContactCreateDalDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ContactUpdateDalDto dto,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid contactId,
        CancellationToken cancellationToken = default);
}
