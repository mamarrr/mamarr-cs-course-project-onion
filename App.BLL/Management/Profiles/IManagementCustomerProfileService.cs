namespace App.BLL.Management;

public interface IManagementCustomerProfileService
{
    Task<CustomerProfileModel?> GetProfileAsync(
        ManagementCustomerDashboardContext context,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> UpdateProfileAsync(
        ManagementCustomerDashboardContext context,
        CustomerProfileUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<ProfileOperationResult> DeleteProfileAsync(
        ManagementCustomerDashboardContext context,
        CancellationToken cancellationToken = default);
}

