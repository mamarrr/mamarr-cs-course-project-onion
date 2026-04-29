using App.BLL.Contracts.ManagementCompanies.Models;

namespace App.BLL.Contracts.ManagementCompanies.Services;

public interface IManagementCompanyProfileService
{
    Task<CompanyProfileModel?> GetProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        Guid appUserId,
        string companySlug,
        CompanyProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

