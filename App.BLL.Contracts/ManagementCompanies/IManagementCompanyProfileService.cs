using App.BLL.DTO.ManagementCompanies.Models;
using FluentResults;

namespace App.BLL.Contracts.ManagementCompanies;

public interface IManagementCompanyProfileService
{
    Task<Result<CompanyProfileModel>> GetProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateProfileAsync(
        Guid appUserId,
        string companySlug,
        CompanyProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

