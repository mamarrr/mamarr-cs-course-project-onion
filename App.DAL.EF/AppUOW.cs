using App.DAL.Contracts;
using App.DAL.Contracts.Repositories;
using App.DAL.Contracts.Repositories.Admin;
using App.DAL.EF.Mappers.Admin;
using App.DAL.EF.Mappers.Contacts;
using App.DAL.EF.Mappers.Customers;
using App.DAL.EF.Mappers.Leases;
using App.DAL.EF.Mappers.ManagementCompanies;
using App.DAL.EF.Mappers.Properties;
using App.DAL.EF.Mappers.Residents;
using App.DAL.EF.Mappers.ScheduledWorks;
using App.DAL.EF.Mappers.Tickets;
using App.DAL.EF.Mappers.Units;
using App.DAL.EF.Mappers.Vendors;
using App.DAL.EF.Mappers.WorkLogs;
using App.DAL.EF.Repositories;
using App.DAL.EF.Repositories.Admin;
using Base.DAL.EF;
using Microsoft.EntityFrameworkCore.Storage;

namespace App.DAL.EF;

public class AppUOW : BaseUOW<AppDbContext>, IAppUOW
{
    private IDbContextTransaction? _transaction;
    private readonly AdminUserDalMapper _adminUserMapper = new();
    private readonly AdminCompanyDalMapper _adminCompanyMapper = new();
    private readonly AdminTicketMonitorDalMapper _adminTicketMonitorMapper = new();
    private readonly CustomerDalMapper _customerMapper = new();
    private readonly ContactDalMapper _contactMapper = new();
    private readonly ManagementCompanyDalMapper _managementCompanyMapper = new();
    private readonly ManagementCompanyJoinRequestDalMapper _managementCompanyJoinRequestMapper = new();
    private readonly PropertyDalMapper _propertyMapper = new();
    private readonly ResidentDalMapper _residentMapper = new();
    private readonly ResidentContactDalMapper _residentContactMapper = new();
    private readonly UnitDalMapper _unitMapper = new();
    private readonly LeaseDalMapper _leaseMapper = new();
    private readonly TicketDalMapper _ticketMapper = new();
    private readonly VendorDalMapper _vendorMapper = new();
    private readonly VendorContactDalMapper _vendorContactMapper = new();
    private readonly VendorTicketCategoryDalMapper _vendorTicketCategoryMapper = new();
    private readonly ScheduledWorkDalMapper _scheduledWorkMapper = new();
    private readonly WorkLogDalMapper _workLogMapper = new();
    private IAdminDashboardRepository? _adminDashboard;
    private IAdminUserRepository? _adminUsers;
    private IAdminCompanyRepository? _adminCompanies;
    private IAdminTicketMonitorRepository? _adminTicketMonitor;
    private IContactRepository? _contacts;
    private ICustomerRepository? _customers;
    private IManagementCompanyRepository? _managementCompanies;
    private IManagementCompanyJoinRequestRepository? _managementCompanyJoinRequests;
    private ILookupRepository? _lookups;
    private IPropertyRepository? _properties;
    private IResidentRepository? _residents;
    private IResidentContactRepository? _residentContacts;
    private IUnitRepository? _units;
    private ILeaseRepository? _leases;
    private ITicketRepository? _tickets;
    private IVendorRepository? _vendors;
    private IVendorContactRepository? _vendorContacts;
    private IVendorTicketCategoryRepository? _vendorTicketCategories;
    private IScheduledWorkRepository? _scheduledWorks;
    private IWorkLogRepository? _workLogs;

    public AppUOW(AppDbContext dbContext) : base(dbContext)
    {
    }

    public IAdminDashboardRepository AdminDashboard =>
        _adminDashboard ??= new AdminDashboardRepository(UowDbContext);

    public IAdminUserRepository AdminUsers =>
        _adminUsers ??= new AdminUserRepository(UowDbContext, _adminUserMapper);

    public IAdminCompanyRepository AdminCompanies =>
        _adminCompanies ??= new AdminCompanyRepository(UowDbContext, _adminCompanyMapper);

    public IAdminTicketMonitorRepository AdminTicketMonitor =>
        _adminTicketMonitor ??= new AdminTicketMonitorRepository(UowDbContext, _adminTicketMonitorMapper);

    public ICustomerRepository Customers => _customers ??= new CustomerRepository(UowDbContext, _customerMapper);

    public IContactRepository Contacts => _contacts ??= new ContactRepository(UowDbContext, _contactMapper);

    public IManagementCompanyRepository ManagementCompanies => _managementCompanies ??= new ManagementCompanyRepository(UowDbContext, _managementCompanyMapper);

    public IManagementCompanyJoinRequestRepository ManagementCompanyJoinRequests =>
        _managementCompanyJoinRequests ??= new ManagementCompanyJoinRequestRepository(UowDbContext, _managementCompanyJoinRequestMapper);

    public ILookupRepository Lookups => _lookups ??= new LookupRepository(UowDbContext);

    public IPropertyRepository Properties => _properties ??= new PropertyRepository(UowDbContext, _propertyMapper);

    public IResidentRepository Residents => _residents ??= new ResidentRepository(UowDbContext, _residentMapper);

    public IResidentContactRepository ResidentContacts =>
        _residentContacts ??= new ResidentContactRepository(UowDbContext, _residentContactMapper);

    public IUnitRepository Units => _units ??= new UnitRepository(UowDbContext, _unitMapper);

    public ILeaseRepository Leases => _leases ??= new LeaseRepository(UowDbContext, _leaseMapper);

    public ITicketRepository Tickets => _tickets ??= new TicketRepository(UowDbContext, _ticketMapper);

    public IVendorRepository Vendors => _vendors ??= new VendorRepository(UowDbContext, _vendorMapper);

    public IVendorContactRepository VendorContacts =>
        _vendorContacts ??= new VendorContactRepository(UowDbContext, _vendorContactMapper);

    public IVendorTicketCategoryRepository VendorTicketCategories =>
        _vendorTicketCategories ??= new VendorTicketCategoryRepository(UowDbContext, _vendorTicketCategoryMapper);

    public IScheduledWorkRepository ScheduledWorks =>
        _scheduledWorks ??= new ScheduledWorkRepository(UowDbContext, _scheduledWorkMapper);

    public IWorkLogRepository WorkLogs =>
        _workLogs ??= new WorkLogRepository(UowDbContext, _workLogMapper);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _transaction = await UowDbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction exists.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction exists.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
