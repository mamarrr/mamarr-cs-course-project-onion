using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Contacts;

public interface IContactService : IBaseService<ContactBllDto>
{
    Task<Result<IReadOnlyList<ContactBllDto>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ContactBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        ContactBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<ContactBllDto>> UpdateAsync(
        ContactRoute route,
        ContactBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ContactRoute route,
        CancellationToken cancellationToken = default);
}
