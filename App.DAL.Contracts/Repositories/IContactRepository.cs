using App.DAL.DTO.Contacts;
using Base.DAL.Contracts;

namespace App.DAL.Contracts.Repositories;

public interface IContactRepository : IBaseRepository<ContactDalDto>
{
}
