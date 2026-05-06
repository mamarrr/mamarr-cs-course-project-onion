using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.ManagementCompanies;
using App.BLL.DTO.ManagementCompanies.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface IManagementCompanyService : IBaseService<ManagementCompanyBllDto>
{
    Task<Result<ManagementCompanyBllDto>> CreateAsync(
        Guid appUserId,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyProfileModel>> CreateAndGetProfileAsync(
        Guid appUserId,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyMembershipContext>> AuthorizeManagementAreaAccessAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyProfileModel>> GetProfileAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementCompanyBllDto>> UpdateAsync(
        ManagementCompanyRoute route,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyProfileModel>> UpdateAndGetProfileAsync(
        ManagementCompanyRoute route,
        ManagementCompanyBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);
}
