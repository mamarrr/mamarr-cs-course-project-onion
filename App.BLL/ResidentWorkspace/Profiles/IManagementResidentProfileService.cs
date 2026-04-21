using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Shared.Profiles;

namespace App.BLL.ResidentWorkspace.Profiles;

public interface IManagementResidentProfileService
{
    Task<ResidentProfileModel?> GetProfileAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        ResidentDashboardContext context,
        ResidentProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        ResidentDashboardContext context,
        CancellationToken cancellationToken = default);
}

