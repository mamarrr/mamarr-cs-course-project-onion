using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Onboarding;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using Base.BLL.Contracts;

namespace App.BLL.Contracts;

public interface IAppBLL : IBaseBLL
{
    ICustomerService Customers { get; }
    IPropertyService Properties { get; }
    IUnitService Units { get; }
    IResidentService Residents { get; }

    IAccountOnboardingService AccountOnboarding { get; }
    IOnboardingCompanyJoinRequestService OnboardingCompanyJoinRequests { get; }
    IWorkspaceContextService WorkspaceContexts { get; }
    IWorkspaceCatalogService WorkspaceCatalog { get; }
    IWorkspaceRedirectService WorkspaceRedirect { get; }
    IContextSelectionService ContextSelection { get; }

    IManagementCompanyProfileService ManagementCompanyProfiles { get; }
    ICompanyMembershipAdminService CompanyMembershipAdmin { get; }

    ILeaseAssignmentService LeaseAssignments { get; }
    ILeaseLookupService LeaseLookups { get; }

    IManagementTicketService ManagementTickets { get; }
}
