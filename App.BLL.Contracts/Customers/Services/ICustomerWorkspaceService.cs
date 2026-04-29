using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using FluentResults;

namespace App.BLL.Contracts.Customers.Services;

public interface ICustomerWorkspaceService
{
    Task<Result<CustomerWorkspaceModel>> GetWorkspaceAsync(
        GetCustomerWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
