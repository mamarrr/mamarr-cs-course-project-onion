using App.BLL.CustomerWorkspace.Workspace;

namespace App.BLL.CustomerWorkspace.Customers;

public interface ICompanyCustomerService
{
    Task<CompanyCustomerListResult> ListAsync(
        CustomerWorkspaceContext context,
        CancellationToken cancellationToken = default);

    Task<CustomerCreateResult> CreateAsync(
        CustomerWorkspaceContext context,
        CustomerCreateRequest request,
        CancellationToken cancellationToken = default);
}

