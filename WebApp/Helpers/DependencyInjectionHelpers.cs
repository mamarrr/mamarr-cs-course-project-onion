using App.BLL.CustomerWorkspace.Access;
using App.BLL.CustomerWorkspace.Customers;
using App.BLL.CustomerWorkspace.Profiles;
using App.BLL.CustomerWorkspace.Workspace;
using App.BLL.LeaseAssignments;
using App.BLL.ManagementCompany.Membership;
using App.BLL.ManagementCompany.Profiles;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.Api;
using App.BLL.Onboarding.CompanyJoinRequests;
using App.BLL.Onboarding.ContextSelection;
using App.BLL.Onboarding.WorkspaceCatalog;
using App.BLL.PropertyWorkspace.Profiles;
using App.BLL.PropertyWorkspace.Properties;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.UnitWorkspace.Access;
using App.BLL.UnitWorkspace.Profiles;
using App.BLL.UnitWorkspace.Units;
using App.BLL.UnitWorkspace.Workspace;
using App.Contracts;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.ApiControllers.Shared;

namespace WebApp.Helpers;

public static class DependencyInjectionHelpers
{
    public static IServiceCollection AddAppDalEf(
        this IServiceCollection services,
        string connectionString)
    {
        services
            .AddDbContext<AppDbContext>(options => options
                .UseNpgsql(
                    connectionString,
                    o => { o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); }
                )
                .ConfigureWarnings(w =>
                    w.Throw(RelationalEventId.MultipleCollectionIncludeWarning)
                )
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            );

        services.AddDatabaseDeveloperPageExceptionFilter();
        services.AddScoped<IAppUOW, AppUOW>();

        return services;
    }

    public static IServiceCollection AddAppBll(this IServiceCollection services)
    {
        services.AddScoped<IAccountOnboardingService, AccountOnboardingService>();
        services.AddScoped<IWorkspaceRedirectService, WorkspaceRedirectService>();
        services.AddScoped<IApiOnboardingContextService, ApiWorkspaceContextService>();
        services.AddScoped<IUserWorkspaceCatalogService, UserWorkspaceCatalogService>();
        services.AddScoped<ICompanyJoinRequestService, CompanyJoinRequestService>();
        services.AddScoped<ICompanyMembershipAdminService, CompanyMembershipAdminService>();
        services.AddScoped<ICustomerWorkspaceService, CustomerWorkspaceService>();
        services.AddScoped<ICustomerAccessService, CustomerWorkspaceService>();
        services.AddScoped<ICompanyCustomerService, CustomerWorkspaceService>();
        services.AddScoped<IPropertyWorkspaceService, CustomerWorkspaceService>();
        services.AddScoped<IResidentAccessService, ResidentAccessService>();
        services.AddScoped<ICompanyResidentService, CompanyResidentService>();
        services.AddScoped<IPropertyUnitService, UnitWorkspaceService>();
        services.AddScoped<IUnitAccessService, UnitWorkspaceService>();
        services.AddScoped<ILeaseAssignmentService, LeaseAssignmentService>();
        services.AddScoped<ILeaseLookupService, LeaseLookupService>();
        services.AddScoped<IManagementCompanyProfileService, ManagementCompanyProfileService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IPropertyProfileService, PropertyProfileService>();
        services.AddScoped<IUnitProfileService, UnitProfileService>();
        services.AddScoped<IResidentProfileService, ResidentProfileService>();

        return services;
    }

    public static IServiceCollection AddWebAppMappers(this IServiceCollection services)
    {
        services.AddScoped<IApiOnboardingRouteContextMapper, ApiOnboardingRouteContextMapper>();

        return services;
    }
}
