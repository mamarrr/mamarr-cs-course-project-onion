namespace App.BLL.Management;

public interface IManagementCompanyProfileService
{
    Task<ManagementCompanyProfileModel?> GetProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        Guid appUserId,
        string companySlug,
        ManagementCompanyProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        Guid appUserId,
        string companySlug,
        CancellationToken cancellationToken = default);
}

