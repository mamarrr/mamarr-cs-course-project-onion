using App.BLL.Shared.Profiles;
using App.BLL.UnitWorkspace.Workspace;

namespace App.BLL.UnitWorkspace.Profiles;

public interface IManagementUnitProfileService
{
    Task<UnitProfileModel?> GetProfileAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        UnitDashboardContext context,
        UnitProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        UnitDashboardContext context,
        CancellationToken cancellationToken = default);
}

