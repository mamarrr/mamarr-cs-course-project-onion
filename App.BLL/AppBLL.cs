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
using App.BLL.Services.Onboarding.Account;
using App.BLL.Services.Onboarding.CompanyJoinRequests;
using App.BLL.Services.Onboarding.ContextSelection;
using App.BLL.Services.Onboarding.WorkspaceContext;
using App.BLL.Services.Onboarding.WorkspaceCatalog;
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
    private IAccountOnboardingService? _accountOnboarding;
    private IOnboardingCompanyJoinRequestService? _onboardingCompanyJoinRequests;
    private IWorkspaceContextService? _workspaceContexts;
    private IWorkspaceCatalogService? _workspaceCatalog;
    private IWorkspaceRedirectService? _workspaceRedirect;
    private IManagementCompanyProfileService? _managementCompanyProfiles;
    private ICompanyMembershipAdminService? _companyMembershipAdmin;
    private IPropertyService? _properties;
    private IResidentService? _residents;
    private IUnitService? _units;
    private ILeaseAssignmentService? _leaseAssignments;
    private ILeaseLookupService? _leaseLookups;
    private IManagementTicketService? _managementTickets;
    private IAppDeleteGuard? _deleteGuard;

    
    private IAppDeleteGuard DeleteGuard =>
        _deleteGuard ??= new AppDeleteGuard(UOW);

    public IAccountOnboardingService AccountOnboarding =>
        _accountOnboarding ??= new AccountOnboardingService(UOW);

    public IOnboardingCompanyJoinRequestService OnboardingCompanyJoinRequests =>
        _onboardingCompanyJoinRequests ??= new OnboardingCompanyJoinRequestService(UOW);

    public IWorkspaceContextService WorkspaceContexts =>
        _workspaceContexts ??= new WorkspaceContextService(UOW, AccountOnboarding);

    public IWorkspaceCatalogService WorkspaceCatalog =>
        _workspaceCatalog ??= new UserWorkspaceCatalogService(UOW);

    public IWorkspaceRedirectService WorkspaceRedirect =>
        _workspaceRedirect ??= new WorkspaceRedirectService(UOW, AccountOnboarding, WorkspaceCatalog);

    public IContextSelectionService ContextSelection => (IContextSelectionService) WorkspaceRedirect;

    public IManagementCompanyProfileService ManagementCompanyProfiles =>
        _managementCompanyProfiles ??= new ManagementCompanyProfileService(UOW, CompanyMembershipAdmin);

    public ICompanyMembershipAdminService CompanyMembershipAdmin =>
        _companyMembershipAdmin ??= new CompanyMembershipAdminService(UOW);

    public ICustomerService Customers =>
        _customers ??= new CustomerService(
            UOW,
            DeleteGuard);

    public IPropertyService Properties =>
        _properties ??= new PropertyService(UOW, Customers, DeleteGuard);

    public IResidentService Residents =>
        _residents ??= new ResidentService(UOW, DeleteGuard);

    public ILeaseAssignmentService LeaseAssignments =>
        _leaseAssignments ??= new LeaseAssignmentService(UOW);

    public ILeaseLookupService LeaseLookups =>
        _leaseLookups ??= new LeaseLookupService(UOW);

    public IManagementTicketService ManagementTickets =>
        _managementTickets ??= new ManagementTicketService(Customers, UOW, DeleteGuard);

    public IUnitService Units =>
        _units ??= new UnitService(UOW, Properties, DeleteGuard);

    public AppBLL(
        IAppUOW uow) : base(uow)
    {
    }
}
