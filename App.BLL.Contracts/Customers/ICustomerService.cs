using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Customers;
using App.BLL.DTO.Customers.Models;
using Base.BLL.Contracts;
using FluentResults;

namespace App.BLL.Contracts.Customers;

public interface ICustomerService : IBaseService<CustomerBllDto>
{
    Task<Result<CompanyWorkspaceModel>> ResolveCompanyWorkspaceAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CustomerListItemModel>>> ListForCompanyAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerWorkspaceModel>> ResolveWorkspaceAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerProfileModel>> GetProfileAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerBllDto>> CreateAsync(
        ManagementCompanyRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerProfileModel>> CreateAndGetProfileAsync(
        ManagementCompanyRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerBllDto>> UpdateAsync(
        CustomerRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerProfileModel>> UpdateAndGetProfileAsync(
        CustomerRoute route,
        CustomerBllDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        CustomerRoute route,
        string confirmationName,
        CancellationToken cancellationToken = default);
}
