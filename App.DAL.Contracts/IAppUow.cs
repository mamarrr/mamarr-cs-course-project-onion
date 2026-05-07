using App.DAL.Contracts.Repositories;
using Base.DAL.Contracts;


namespace App.DAL.Contracts;

public interface IAppUOW : IBaseUOW
{
    ICustomerRepository Customers { get; }

    IManagementCompanyRepository ManagementCompanies { get; }

    IManagementCompanyJoinRequestRepository ManagementCompanyJoinRequests { get; }

    ILookupRepository Lookups { get; }

    IPropertyRepository Properties { get; }

    IResidentRepository Residents { get; }

    IResidentContactRepository ResidentContacts { get; }

    IContactRepository Contacts { get; }

    IUnitRepository Units { get; }

    ILeaseRepository Leases { get; }

    ITicketRepository Tickets { get; }

    IVendorRepository Vendors { get; }

    IVendorContactRepository VendorContacts { get; }

    IVendorTicketCategoryRepository VendorTicketCategories { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
