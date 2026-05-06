using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using FluentResults;

namespace App.BLL.Contracts.Customers;

public interface ICustomerWorkspaceService
{
    Task<Result<CustomerWorkspaceModel>> GetWorkspaceAsync(
        GetCustomerWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
