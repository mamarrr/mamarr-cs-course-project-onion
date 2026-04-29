using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using FluentResults;

namespace App.BLL.Contracts.Customers.Services;

public interface ICustomerAccessService
{
    Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        GetCompanyCustomersQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerWorkspaceModel>> ResolveCustomerWorkspaceAsync(
        GetCustomerWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
