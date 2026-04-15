namespace App.BLL.Management;

public interface IManagementCustomerService
{
    Task<ManagementCustomerListResult> ListAsync(
        ManagementCustomersAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<ManagementCustomerCreateResult> CreateAsync(
        ManagementCustomersAuthorizedContext context,
        ManagementCustomerCreateRequest request,
        CancellationToken cancellationToken = default);
}

