using App.BLL;
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
using App.BLL.Services.Common.Deletion;
using App.BLL.Services.Customers;
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
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.Mappers.Mvc.Customers;
using WebApp.Mappers.Mvc.Leases;
using WebApp.Mappers.Mvc.Onboarding;
using WebApp.Mappers.Mvc.Properties;
using WebApp.Mappers.Mvc.Residents;
using WebApp.Mappers.Mvc.Tickets;
using WebApp.Mappers.Mvc.Units;
using WebApp.Services.Identity;

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
        services.AddScoped<IAppBLL, AppBLL>();
        services.AddScoped<IAccountOnboardingService, AccountOnboardingService>();
        services.AddScoped<IWorkspaceRedirectService, WorkspaceRedirectService>();
        services.AddScoped<IContextSelectionService, WorkspaceRedirectService>();
        services.AddScoped<IWorkspaceContextService, WorkspaceContextService>();
        services.AddScoped<IWorkspaceCatalogService, UserWorkspaceCatalogService>();
        services.AddScoped<IOnboardingCompanyJoinRequestService, OnboardingCompanyJoinRequestService>();
        services.AddScoped<IAppDeleteGuard, AppDeleteGuard>();
        services.AddScoped<ICompanyMembershipAdminService, CompanyMembershipAdminService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IResidentService, ResidentService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<ILeaseAssignmentService, LeaseAssignmentService>();
        services.AddScoped<ILeaseLookupService, LeaseLookupService>();
        services.AddScoped<IManagementCompanyProfileService, ManagementCompanyProfileService>();
        services.AddScoped<IManagementTicketService, ManagementTicketService>();

        return services;
    }

    public static IServiceCollection AddWebAppMappers(this IServiceCollection services)
    {
        services.AddScoped<CustomerProfileMvcMapper>();
        services.AddScoped<CustomerWorkspaceMvcMapper>();
        services.AddScoped<CompanyCustomerMvcMapper>();
        services.AddScoped<PropertyMvcMapper>();
        services.AddScoped<UnitMvcMapper>();
        services.AddScoped<ResidentMvcMapper>();
        services.AddScoped<LeaseViewModelMapper>();
        services.AddScoped<ManagementTicketMvcMapper>();
        services.AddScoped<OnboardingViewModelMapper>();

        return services;
    }
}
