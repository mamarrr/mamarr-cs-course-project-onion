using Base.DAL.Contracts;
using App.Contracts.DAL.Contacts;
using App.Contracts.DAL.Customers;
using App.Contracts.DAL.Leases;
using App.Contracts.DAL.Lookups;
using App.Contracts.DAL.ManagementCompanies;
using App.Contracts.DAL.Properties;
using App.Contracts.DAL.Residents;
using App.Contracts.DAL.Tickets;
using App.Contracts.DAL.Units;

namespace App.Contracts;

public interface IAppUOW : IBaseUOW
{
    ICustomerRepository Customers { get; }

    IManagementCompanyRepository ManagementCompanies { get; }

    IManagementCompanyJoinRequestRepository ManagementCompanyJoinRequests { get; }

    ILookupRepository Lookups { get; }

    IPropertyRepository Properties { get; }

    IResidentRepository Residents { get; }

    IContactRepository Contacts { get; }

    IUnitRepository Units { get; }

    ILeaseRepository Leases { get; }

    ITicketRepository Tickets { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
