using App.DAL.Contracts.Repositories;
using App.DAL.Contracts.Repositories.Admin;
using App.DAL.Contracts.Repositories.Dashboards;
using App.DAL.Contracts.Repositories.Identity;
using Base.DAL.Contracts;


namespace App.DAL.Contracts;

public interface IAppUOW : IBaseUOW
{
    IAdminDashboardRepository AdminDashboard { get; }

    IAdminUserRepository AdminUsers { get; }

    IAdminCompanyRepository AdminCompanies { get; }

    IAdminTicketMonitorRepository AdminTicketMonitor { get; }

    IPortalDashboardRepository PortalDashboards { get; }

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

    IScheduledWorkRepository ScheduledWorks { get; }

    IWorkLogRepository WorkLogs { get; }

    IAppRefreshTokenRepository RefreshTokens { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
