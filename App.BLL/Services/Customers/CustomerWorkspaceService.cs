using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using FluentResults;

namespace App.BLL.Services.Customers;

public class CustomerWorkspaceService : ICustomerWorkspaceService
{
    private readonly ICustomerAccessService _customerAccessService;

    public CustomerWorkspaceService(ICustomerAccessService customerAccessService)
    {
        _customerAccessService = customerAccessService;
    }

    public Task<Result<CustomerWorkspaceModel>> GetWorkspaceAsync(
        GetCustomerWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        return _customerAccessService.ResolveCustomerWorkspaceAsync(query, cancellationToken);
    }
}
