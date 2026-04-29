using App.BLL.Contracts.Customers.Services;
using App.BLL.Customers;
using App.BLL.LeaseAssignments;
using App.BLL.ManagementCompany.Membership;
using App.BLL.ManagementCompany.Profiles;
using App.BLL.Onboarding.Account;
using App.BLL.Onboarding.Api;
using App.BLL.Onboarding.CompanyJoinRequests;
using App.BLL.Onboarding.ContextSelection;
using App.BLL.Onboarding.WorkspaceCatalog;
using App.BLL.Properties;
using App.BLL.Contracts.Properties.Services;
using App.BLL.ResidentWorkspace.Access;
using App.BLL.ResidentWorkspace.Profiles;
using App.BLL.ResidentWorkspace.Residents;
using App.BLL.Units;
using App.BLL.Contracts.Units.Services;
using App.Contracts;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WebApp.ApiControllers.Shared;
using WebApp.Mappers.Api.Customers;
using WebApp.Mappers.Api.Properties;
using WebApp.Mappers.Api.Residents;
using WebApp.Mappers.Api.Units;
using WebApp.Mappers.Mvc.Customers;
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
        services.AddScoped<App.BLL.Contracts.Residents.Services.IResidentAccessService, App.BLL.Residents.ResidentAccessService>();
        services.AddScoped<App.BLL.Contracts.Residents.Services.IResidentWorkspaceService, App.BLL.Residents.ResidentWorkspaceService>();
        services.AddScoped<App.BLL.Contracts.Residents.Services.IResidentProfileService, App.BLL.Residents.ResidentProfileService>();
        services.AddScoped<IResidentAccessService, ResidentAccessService>();
        services.AddScoped<ICompanyResidentService, CompanyResidentService>();
        services.AddScoped<IUnitAccessService, UnitAccessService>();
        services.AddScoped<IUnitWorkspaceService, UnitWorkspaceService>();
        services.AddScoped<ILeaseAssignmentService, LeaseAssignmentService>();
        services.AddScoped<ILeaseLookupService, LeaseLookupService>();
        services.AddScoped<IManagementCompanyProfileService, ManagementCompanyProfileService>();
        services.AddScoped<IPropertyWorkspaceService, PropertyWorkspaceService>();
        services.AddScoped<IPropertyProfileService, PropertyProfileService>();
        services.AddScoped<IUnitProfileService, UnitProfileService>();
        services.AddScoped<IResidentProfileService, ResidentProfileService>();

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
        services.AddScoped<CustomerProfileMvcMapper>();
        services.AddScoped<CompanyCustomerMvcMapper>();
        services.AddScoped<PropertyMvcMapper>();
        services.AddScoped<UnitMvcMapper>();
        services.AddScoped<ResidentMvcMapper>();

        return services;
    }
}
