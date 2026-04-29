using App.BLL.Contracts.Customers.Services;
using App.BLL.Contracts.Leases.Services;
using App.BLL.Contracts.ManagementCompanies.Services;
using App.BLL.Customers;
using App.BLL.Leases;
using App.BLL.ManagementCompanies;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.Api;
using App.BLL.Onboarding.CompanyJoinRequests;
using App.BLL.Onboarding.ContextSelection;
using App.BLL.Onboarding.WorkspaceCatalog;
using App.BLL.Properties;
using App.BLL.Contracts.Properties.Services;
using App.BLL.Contracts.Residents.Services;
using App.BLL.Units;
using App.BLL.Contracts.Units.Services;
using App.BLL.Residents;
using App.Contracts;
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
using WebApp.Mappers.Mvc.Properties;
using WebApp.Mappers.Mvc.Residents;
using WebApp.Mappers.Mvc.Units;

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
        services.AddScoped<CompanyCustomerMvcMapper>();
        services.AddScoped<PropertyMvcMapper>();
        services.AddScoped<UnitMvcMapper>();
        services.AddScoped<ResidentMvcMapper>();
        services.AddScoped<LeaseViewModelMapper>();

        return services;
    }
}
