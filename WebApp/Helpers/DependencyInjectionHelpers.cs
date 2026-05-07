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
using App.BLL.Services.Onboarding;
using App.BLL.Services.Properties;
using App.BLL.Services.Residents;
using App.BLL.Services.Tickets;
using App.BLL.Services.Units;
using App.DAL.Contracts;
using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IAppDeleteGuard, AppDeleteGuard>();
        services.AddScoped<ICompanyMembershipService, CompanyMembershipService>();
        services.AddScoped<IManagementCompanyService, ManagementCompanyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IResidentService, ResidentService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<ILeaseService, LeaseService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }

}
