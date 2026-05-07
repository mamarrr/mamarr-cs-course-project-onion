using App.BLL.Contracts.Admin;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Workspace;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Vendors;
using Base.BLL.Contracts;

namespace App.BLL.Contracts;

public interface IAppBLL : IBaseBLL
{
    IAdminDashboardService AdminDashboard { get; }
    IAdminUserService AdminUsers { get; }
    IAdminCompanyService AdminCompanies { get; }
    IAdminLookupService AdminLookups { get; }
    IAdminTicketMonitorService AdminTicketMonitor { get; }
    ICustomerService Customers { get; }
    IPropertyService Properties { get; }
    IUnitService Units { get; }
    IResidentService Residents { get; }
    ILeaseService Leases { get; }
    ITicketService Tickets { get; }
    IVendorService Vendors { get; }
    IManagementCompanyService ManagementCompanies { get; }
    ICompanyMembershipService CompanyMemberships { get; }
    IWorkspaceService Workspaces { get; }
}
