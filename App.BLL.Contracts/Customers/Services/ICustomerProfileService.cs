using App.BLL.Contracts.Customers.Commands;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using FluentResults;

namespace App.BLL.Contracts.Customers.Services;

public interface ICustomerProfileService
{
    Task<Result<CustomerProfileModel>> GetAsync(
        GetCustomerProfileQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerProfileModel>> UpdateAsync(
        UpdateCustomerProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        DeleteCustomerCommand command,
        CancellationToken cancellationToken = default);
}
