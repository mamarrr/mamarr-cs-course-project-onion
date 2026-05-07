using App.BLL.Contracts;
using App.BLL.Contracts.Common.Deletion;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Onboarding;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using App.BLL.Services.Customers;
using App.BLL.Services.Common.Deletion;
using App.BLL.Services.Leases;
using App.BLL.Services.ManagementCompanies;
using App.BLL.Services.Onboarding;
using App.BLL.Services.Properties;
using App.BLL.Services.Residents;
using App.BLL.Services.Tickets;
using App.BLL.Services.Units;
using App.DAL.Contracts;
using Base.BLL;


namespace App.BLL;

public class AppBLL : BaseBLL<IAppUOW>, IAppBLL
{
    private ICustomerService? _customers;
    private IWorkspaceService? _workspaces;
    private IManagementCompanyService? _managementCompanies;
    private ICompanyMembershipService? _companyMemberships;
    private IPropertyService? _properties;
    private IResidentService? _residents;
    private IUnitService? _units;
    private ILeaseService? _leases;
    private ITicketService? _tickets;
    private IAppDeleteGuard? _deleteGuard;

    
    private IAppDeleteGuard DeleteGuard =>
        _deleteGuard ??= new AppDeleteGuard(UOW);

    public IWorkspaceService Workspaces =>
        _workspaces ??= new WorkspaceService(UOW);

    public ICompanyMembershipService CompanyMemberships =>
        _companyMemberships ??= new CompanyMembershipService(UOW);

    public IManagementCompanyService ManagementCompanies =>
        _managementCompanies ??= new ManagementCompanyService(UOW, CompanyMemberships);

    public ICustomerService Customers =>
        _customers ??= new CustomerService(
            UOW,
            DeleteGuard);

    public IPropertyService Properties =>
        _properties ??= new PropertyService(UOW, Customers, DeleteGuard);

    public IResidentService Residents =>
        _residents ??= new ResidentService(UOW, DeleteGuard);

    public ILeaseService Leases =>
        _leases ??= new LeaseService(UOW, Residents, Units);

    public ITicketService Tickets =>
        _tickets ??= new TicketService(Customers, UOW, DeleteGuard);

    public IUnitService Units =>
        _units ??= new UnitService(UOW, Properties, DeleteGuard);

    public AppBLL(
        IAppUOW uow) : base(uow)
    {
    }
}
