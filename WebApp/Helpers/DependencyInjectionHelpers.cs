using App.BLL;
using App.BLL.Contracts;
using App.BLL.Contracts.Common.Portal;
using App.BLL.Contracts.Contacts;
using App.BLL.Contracts.Customers;
using App.BLL.Contracts.Leases;
using App.BLL.Contracts.ManagementCompanies;
using App.BLL.Contracts.Workspace;
using App.BLL.Contracts.Properties;
using App.BLL.Contracts.Residents;
using App.BLL.Contracts.Tickets;
using App.BLL.Contracts.Units;
using App.BLL.Contracts.Vendors;
using App.BLL.Services.Common.Portal;
using App.BLL.Services.Contacts;
using App.BLL.Services.Customers;
using App.BLL.Services.Leases;
using App.BLL.Services.ManagementCompanies;
using App.BLL.Services.Workspace;
using App.BLL.Services.Properties;
using App.BLL.Services.Residents;
using App.BLL.Services.Tickets;
using App.BLL.Services.Units;
using App.BLL.Services.Vendors;
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
        services.AddScoped<IPortalContextProvider, PortalContextProvider>();
        services.AddScoped<ContactWriter>();
        services.AddScoped<ICompanyMembershipService, CompanyMembershipService>();
        services.AddScoped<IManagementCompanyService, ManagementCompanyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IResidentService, ResidentService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<ILeaseService, LeaseService>();
        services.AddScoped<IScheduledWorkService, ScheduledWorkService>();
        services.AddScoped<IWorkLogService, WorkLogService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IVendorService, VendorService>();

        return services;
    }

}
