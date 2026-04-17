namespace App.BLL.Management;

public interface IManagementResidentProfileService
{
    Task<ResidentProfileModel?> GetProfileAsync(
        ManagementResidentDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementResidentDashboardContext context,
        ResidentProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementResidentDashboardContext context,
        CancellationToken cancellationToken = default);
}

