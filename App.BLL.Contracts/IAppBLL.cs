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
    IAccountOnboardingService AccountOnboarding { get; }
    IWorkspaceCatalogService WorkspaceCatalog { get; }
    IWorkspaceRedirectService WorkspaceRedirect { get; }

    IManagementCompanyProfileService ManagementCompanyProfiles { get; }
    ICompanyMembershipAdminService CompanyMembershipAdmin { get; }

    ICompanyCustomerService CompanyCustomers { get; }
    ICustomerProfileService CustomerProfiles { get; }
    ICustomerWorkspaceService CustomerWorkspaces { get; }

    IPropertyProfileService PropertyProfiles { get; }
    IPropertyWorkspaceService PropertyWorkspaces { get; }

    IResidentProfileService ResidentProfiles { get; }
    IResidentWorkspaceService ResidentWorkspaces { get; }

    IUnitProfileService UnitProfiles { get; }
    IUnitWorkspaceService UnitWorkspaces { get; }

    ILeaseAssignmentService LeaseAssignments { get; }
    ILeaseLookupService LeaseLookups { get; }

    IManagementTicketService ManagementTickets { get; }
}