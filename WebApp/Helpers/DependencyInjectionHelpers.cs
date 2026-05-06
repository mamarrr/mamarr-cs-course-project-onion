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
using App.BLL.Services.Onboarding.Api;
using App.BLL.Services.Onboarding.CompanyJoinRequests;
using App.BLL.Services.Onboarding.ContextSelection;
using App.BLL.Services.Onboarding.WorkspaceCatalog;
using App.BLL.Services.Properties;
using App.BLL.Services.Residents;
using App.BLL.Services.Tickets;
using App.BLL.Services.Units;
using App.DAL.Contracts;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.ApiControllers.Shared;
using WebApp.Mappers.Api.Customers;
using WebApp.Mappers.Api.Leases;
using WebApp.Mappers.Api.Properties;
using WebApp.Mappers.Api.Residents;
using WebApp.Mappers.Api.Units;
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
        services.AddScoped<IApiOnboardingContextService, ApiWorkspaceContextService>();
        services.AddScoped<IWorkspaceCatalogService, UserWorkspaceCatalogService>();
        services.AddScoped<IOnboardingCompanyJoinRequestService, OnboardingCompanyJoinRequestService>();
        services.AddScoped<IAppDeleteGuard, AppDeleteGuard>();
        services.AddScoped<ICompanyMembershipAdminService, CompanyMembershipAdminService>();
        services.AddScoped<ICompanyCustomerService, CompanyCustomerService>();
        services.AddScoped<ICustomerAccessService, CustomerAccessService>();
        services.AddScoped<ICustomerWorkspaceService, CustomerWorkspaceService>();
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IResidentAccessService, ResidentAccessService>();
        services.AddScoped<IResidentWorkspaceService, ResidentWorkspaceService>();
        services.AddScoped<IResidentProfileService, ResidentProfileService>();
        services.AddScoped<IUnitAccessService, UnitAccessService>();
        services.AddScoped<IUnitWorkspaceService, UnitWorkspaceService>();
        services.AddScoped<ILeaseAssignmentService, LeaseAssignmentService>();
        services.AddScoped<ILeaseLookupService, LeaseLookupService>();
        services.AddScoped<IManagementCompanyProfileService, ManagementCompanyProfileService>();
        services.AddScoped<IPropertyWorkspaceService, PropertyWorkspaceService>();
        services.AddScoped<IPropertyProfileService, PropertyProfileService>();
        services.AddScoped<IUnitProfileService, UnitProfileService>();
        services.AddScoped<IManagementTicketService, ManagementTicketService>();

        return services;
    }

    public static IServiceCollection AddWebAppMappers(this IServiceCollection services)
    {
        services.AddScoped<IApiOnboardingRouteContextMapper, ApiOnboardingRouteContextMapper>();
        services.AddScoped<CustomerProfileApiMapper>();
        services.AddScoped<CustomerWorkspaceApiMapper>();
        services.AddScoped<CompanyCustomerApiMapper>();
        services.AddScoped<PropertyApiMapper>();
        services.AddScoped<UnitApiMapper>();
        services.AddScoped<ResidentApiMapper>();
        services.AddScoped<LeaseApiMapper>();
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
