using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Profiles;

namespace App.BLL.ResidentWorkspace.Profiles;

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

