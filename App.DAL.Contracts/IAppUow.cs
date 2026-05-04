using Base.DAL.Contracts;
using App.DAL.Contracts.DAL.Contacts;
using App.DAL.Contracts.DAL.Customers;
using App.DAL.Contracts.DAL.Leases;
using App.DAL.Contracts.DAL.Lookups;
using App.DAL.Contracts.DAL.ManagementCompanies;
using App.DAL.Contracts.DAL.Properties;
using App.DAL.Contracts.DAL.Residents;
using App.DAL.Contracts.DAL.Tickets;
using App.DAL.Contracts.DAL.Units;

namespace App.DAL.Contracts;

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
