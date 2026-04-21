using App.BLL.CustomerWorkspace.Workspace;

namespace App.BLL.CustomerWorkspace.Customers;

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

