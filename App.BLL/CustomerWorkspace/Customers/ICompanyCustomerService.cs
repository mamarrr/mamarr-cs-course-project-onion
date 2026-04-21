using App.BLL.CustomerWorkspace.Workspace;

namespace App.BLL.CustomerWorkspace.Customers;

public interface ICompanyCustomerService
{
    Task<CompanyCustomerListResult> ListAsync(
        CustomerWorkspaceAuthorizedContext context,
        CancellationToken cancellationToken = default);

    Task<CustomerCreateResult> CreateAsync(
        CustomerWorkspaceAuthorizedContext context,
        CustomerCreateRequest request,
        CancellationToken cancellationToken = default);
}

