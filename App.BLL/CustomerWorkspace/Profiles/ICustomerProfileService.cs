using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.Shared.Profiles;

namespace App.BLL.CustomerWorkspace.Profiles;

public interface ICustomerProfileService
{
    Task<CustomerProfileModel?> GetProfileAsync(
        CustomerWorkspaceDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        CustomerWorkspaceDashboardContext context,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        CustomerWorkspaceDashboardContext context,
        CancellationToken cancellationToken = default);
}

