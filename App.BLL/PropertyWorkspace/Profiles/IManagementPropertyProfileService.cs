using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Profiles;

namespace App.BLL.PropertyWorkspace.Profiles;

public interface IManagementPropertyProfileService
{
    Task<PropertyProfileModel?> GetProfileAsync(
        ManagementCustomerPropertyDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementCustomerPropertyDashboardContext context,
        PropertyProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementCustomerPropertyDashboardContext context,
        CancellationToken cancellationToken = default);
}

