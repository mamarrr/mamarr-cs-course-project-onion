using App.BLL.Contracts;
using App.BLL.Contracts.Admin;
using App.BLL.Contracts.Common.Portal;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Dashboards;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Workspace;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Vendors;
using App.BLL.Services.Admin;
using App.BLL.Services.Contacts;
using App.BLL.Services.Common.Portal;
using App.BLL.Services.Customers;
using App.BLL.Services.Dashboards;
using App.BLL.Services.Leases;
using App.BLL.Services.ManagementCompanies;
using App.BLL.Services.Workspace;
using App.BLL.Services.Properties;
using App.BLL.Services.Residents;
using App.BLL.Services.Tickets;
using App.BLL.Services.Units;
using App.BLL.Services.Vendors;
using App.DAL.Contracts;
using Base.BLL;

namespace App.BLL;

public class AppBLL : BaseBLL<IAppUOW>, IAppBLL
{
    private IAdminDashboardService? _adminDashboard;
    private IAdminUserService? _adminUsers;
    private IAdminCompanyService? _adminCompanies;
    private IAdminLookupService? _adminLookups;
    private IAdminTicketMonitorService? _adminTicketMonitor;
    private IPortalDashboardService? _portalDashboards;
    private ICustomerService? _customers;
    private IWorkspaceService? _workspaces;
    private IManagementCompanyService? _managementCompanies;
    private ICompanyMembershipService? _companyMemberships;
    private IPropertyService? _properties;
    private IResidentService? _residents;
    private IUnitService? _units;
    private ILeaseService? _leases;
    private ITicketService? _tickets;
    private IScheduledWorkService? _scheduledWorks;
    private IWorkLogService? _workLogs;
    private IVendorService? _vendors;
    private IPortalContextProvider? _portalContext;
    private ContactWriter? _contactWriter;

    private IPortalContextProvider PortalContext =>
        _portalContext ??= new PortalContextProvider(UOW);

    private ContactWriter ContactWriter =>
        _contactWriter ??= new ContactWriter(UOW);

    public IAdminDashboardService AdminDashboard =>
        _adminDashboard ??= new AdminDashboardService(UOW);

    public IAdminUserService AdminUsers =>
        _adminUsers ??= new AdminUserService(UOW);

    public IAdminCompanyService AdminCompanies =>
        _adminCompanies ??= new AdminCompanyService(UOW);

    public IAdminLookupService AdminLookups =>
        _adminLookups ??= new AdminLookupService(UOW);

    public IAdminTicketMonitorService AdminTicketMonitor =>
        _adminTicketMonitor ??= new AdminTicketMonitorService(UOW);

    public IPortalDashboardService PortalDashboards =>
        _portalDashboards ??= new PortalDashboardService(UOW, PortalContext);

    public IWorkspaceService Workspaces =>
        _workspaces ??= new WorkspaceService(UOW);

    public ICompanyMembershipService CompanyMemberships =>
        _companyMemberships ??= new CompanyMembershipService(UOW);

    public IManagementCompanyService ManagementCompanies =>
        _managementCompanies ??= new ManagementCompanyService(UOW, PortalContext);

    public ICustomerService Customers =>
        _customers ??= new CustomerService(
            UOW,
            PortalContext);

    public IPropertyService Properties =>
        _properties ??= new PropertyService(UOW, PortalContext);

    public IResidentService Residents =>
        _residents ??= new ResidentService(UOW, PortalContext, ContactWriter);

    public ILeaseService Leases =>
        _leases ??= new LeaseService(UOW, PortalContext);

    public ITicketService Tickets =>
        _tickets ??= new TicketService(UOW);

    public IScheduledWorkService ScheduledWorks =>
        _scheduledWorks ??= new ScheduledWorkService(UOW);

    public IWorkLogService WorkLogs =>
        _workLogs ??= new WorkLogService(UOW);

    public IVendorService Vendors =>
        _vendors ??= new VendorService(UOW, PortalContext, ContactWriter);

    public IUnitService Units =>
        _units ??= new UnitService(UOW, PortalContext);

    public AppBLL(
        IAppUOW uow) : base(uow)
    {
    }
}
