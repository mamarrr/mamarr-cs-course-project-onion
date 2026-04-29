using Base.DAL.Contracts;

namespace App.Contracts.DAL.Contacts;

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
