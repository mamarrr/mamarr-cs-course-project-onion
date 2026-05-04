using App.BLL.Contracts;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Onboarding;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using App.BLL.Services.Customers;
using App.BLL.Services.Leases;
using App.BLL.Services.ManagementCompanies;
using App.BLL.Services.Onboarding.Account;
using App.BLL.Services.Onboarding.ContextSelection;
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
    private ICustomerAccessService? _customerAccess;
    private ICustomerProfileService? _customerProfiles;
    private ICustomerWorkspaceService? _customerWorkspaces;
    private IAccountIdentityService? _accountIdentity;
    private IAccountOnboardingService? _accountOnboarding;
    private IWorkspaceCatalogService? _workspaceCatalog;
    private IWorkspaceRedirectService? _workspaceRedirect;
    private IManagementCompanyProfileService? _managementCompanyProfiles;
    private ICompanyMembershipAdminService? _companyMembershipAdmin;
    private ICompanyCustomerService? _companyCustomers;
    private IPropertyProfileService? _propertyProfiles;
    private IPropertyWorkspaceService? _propertyWorkspaces;
    private IResidentAccessService? _residentAccess;
    private IResidentProfileService? _residentProfiles;
    private IResidentWorkspaceService? _residentWorkspaces;
    private IUnitAccessService? _unitAccess;
    private IUnitProfileService? _unitProfiles;
    private IUnitWorkspaceService? _unitWorkspaces;
    private ILeaseAssignmentService? _leaseAssignments;
    private ILeaseLookupService? _leaseLookups;
    private IManagementTicketService? _managementTickets;

    public IAccountIdentityService AccountIdentity =>
        _accountIdentity ?? throw new InvalidOperationException(
            $"{nameof(IAccountIdentityService)} must be provided by the web layer.");

    public IAccountOnboardingService AccountOnboarding =>
        _accountOnboarding ??= new AccountOnboardingService(AccountIdentity, UOW);

    public IWorkspaceCatalogService WorkspaceCatalog =>
        _workspaceCatalog ??= new UserWorkspaceCatalogService(UOW);

    public IWorkspaceRedirectService WorkspaceRedirect =>
        _workspaceRedirect ??= new WorkspaceRedirectService(UOW, AccountOnboarding, WorkspaceCatalog);

    public IManagementCompanyProfileService ManagementCompanyProfiles =>
        _managementCompanyProfiles ??= new ManagementCompanyProfileService(UOW, CompanyMembershipAdmin);

    public ICompanyMembershipAdminService CompanyMembershipAdmin =>
        _companyMembershipAdmin ??= new CompanyMembershipAdminService(UOW);

    public ICompanyCustomerService CompanyCustomers =>
        _companyCustomers ??= new CompanyCustomerService(CustomerAccess, UOW);

    public ICustomerAccessService CustomerAccess =>
        _customerAccess ??= new CustomerAccessService(UOW);

    public ICustomerProfileService CustomerProfiles =>
        _customerProfiles ??= new CustomerProfileService(CustomerAccess, UOW);

    public ICustomerWorkspaceService CustomerWorkspaces =>
        _customerWorkspaces ??= new CustomerWorkspaceService(CustomerAccess);

    public IPropertyProfileService PropertyProfiles =>
        _propertyProfiles ??= new PropertyProfileService(PropertyWorkspaces, UOW);

    public IPropertyWorkspaceService PropertyWorkspaces =>
        _propertyWorkspaces ??= new PropertyWorkspaceService(CustomerAccess, UOW);

    public IResidentProfileService ResidentProfiles =>
        _residentProfiles ??= new ResidentProfileService(ResidentAccess, UOW);

    public IResidentWorkspaceService ResidentWorkspaces =>
        _residentWorkspaces ??= new ResidentWorkspaceService(ResidentAccess, UOW);

    public IUnitProfileService UnitProfiles =>
        _unitProfiles ??= new UnitProfileService(UnitAccess, UOW);

    public IUnitWorkspaceService UnitWorkspaces =>
        _unitWorkspaces ??= new UnitWorkspaceService(PropertyWorkspaces, UnitAccess, UOW);

    public ILeaseAssignmentService LeaseAssignments =>
        _leaseAssignments ??= new LeaseAssignmentService(UOW);

    public ILeaseLookupService LeaseLookups =>
        _leaseLookups ??= new LeaseLookupService(UOW);

    public IManagementTicketService ManagementTickets =>
        _managementTickets ??= new ManagementTicketService(CustomerAccess, UOW);

    private IResidentAccessService ResidentAccess =>
        _residentAccess ??= new ResidentAccessService(UOW);

    private IUnitAccessService UnitAccess =>
        _unitAccess ??= new UnitAccessService(UOW);

    public AppBLL(IAppUOW uow) : base(uow)
    {
    }

    public AppBLL(IAppUOW uow, IAccountIdentityService accountIdentity) : base(uow)
    {
        _accountIdentity = accountIdentity;
    }
}
