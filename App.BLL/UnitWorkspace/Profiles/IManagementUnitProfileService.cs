using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Profiles;

public interface IManagementUnitProfileService
{
    Task<UnitProfileModel?> GetProfileAsync(
        ManagementUnitDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementUnitDashboardContext context,
        UnitProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementUnitDashboardContext context,
        CancellationToken cancellationToken = default);
}

