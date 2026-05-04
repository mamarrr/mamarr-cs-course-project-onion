using App.BLL.Contracts.Customers.Commands;
using App.BLL.Contracts.Customers.Models;
using App.BLL.Contracts.Customers.Queries;
using FluentResults;

namespace App.BLL.Contracts.Customers;

public interface ICompanyCustomerService
{
    Task<Result<IReadOnlyList<CustomerListItemModel>>> GetCompanyCustomersAsync(
        GetCompanyCustomersQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<CompanyCustomerModel>> CreateCustomerAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default);
}
