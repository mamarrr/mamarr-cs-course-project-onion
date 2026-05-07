using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Contacts;
using App.BLL.DTO.Residents;
using App.BLL.DTO.Residents.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Residents;

public interface IResidentService : IBaseService<ResidentBllDto>
{
    Task<Result<CompanyResidentsModel>> ResolveCompanyResidentsContextAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentWorkspaceModel>> ResolveWorkspaceAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyResidentsModel>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentDashboardModel>> GetDashboardAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentProfileModel>> GetProfileAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentBllDto>> UpdateAsync(
        ResidentRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentProfileModel>> UpdateAndGetProfileAsync(
        ResidentRoute route,
        ResidentBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ResidentRoute route,
        string confirmationIdCode,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentContactListModel>> ListContactsAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentContactListModel>> AddContactAsync(
        ResidentRoute route,
        ResidentContactBllDto dto,
        ContactBllDto? newContact,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentContactListModel>> UpdateContactAsync(
        ResidentContactRoute route,
        ResidentContactBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> SetPrimaryContactAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken = default);

    Task<Result> ConfirmContactAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken = default);

    Task<Result> UnconfirmContactAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveContactAsync(
        ResidentContactRoute route,
        CancellationToken cancellationToken = default);
}
