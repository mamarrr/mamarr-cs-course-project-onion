using App.BLL.DTO.Customers.Commands;
using App.BLL.DTO.Customers.Models;
using App.BLL.DTO.Customers.Queries;
using FluentResults;

namespace App.BLL.Contracts.Customers;

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
