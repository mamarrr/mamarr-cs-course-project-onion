using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Profiles;

namespace App.BLL.PropertyWorkspace.Profiles;

public interface IManagementPropertyProfileService
{
    Task<PropertyProfileModel?> GetProfileAsync(
        PropertyDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        PropertyDashboardContext context,
        PropertyProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        PropertyDashboardContext context,
        CancellationToken cancellationToken = default);
}

