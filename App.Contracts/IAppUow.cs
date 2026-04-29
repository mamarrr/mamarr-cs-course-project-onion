using Base.DAL.Contracts;
using App.Contracts.DAL.Customers;
using App.Contracts.DAL.Lookups;
using App.Contracts.DAL.ManagementCompanies;

namespace App.Contracts;

public interface IAppUOW : IBaseUOW
{
    ICustomerRepository Customers { get; }

    IManagementCompanyRepository ManagementCompanies { get; }

    ILookupRepository Lookups { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
