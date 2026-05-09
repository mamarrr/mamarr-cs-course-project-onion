using App.BLL.Contracts.Common.Portal;
using App.BLL.Contracts.Dashboards;
using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Dashboards.Models;
using App.BLL.Mappers.Dashboards;
using App.DAL.Contracts;
using App.DAL.DTO.Dashboards;
using FluentResults;

namespace App.BLL.Services.Dashboards;

public class PortalDashboardService : IPortalDashboardService
{
    private const int PreviewLimit = 5;

    private static readonly HashSet<string> ManagementAreaRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "OWNER",
        "MANAGER",
        "FINANCE",
        "SUPPORT"
    };

    private readonly IAppUOW _uow;
    private readonly IPortalContextProvider _portalContext;

    public PortalDashboardService(IAppUOW uow, IPortalContextProvider portalContext)
    {
        _uow = uow;
        _portalContext = portalContext;
    }

    public async Task<Result<ManagementDashboardModel>> GetManagementDashboardAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await _portalContext.ResolveCompanyWorkspaceAsync(
            route,
            ManagementAreaRoleCodes,
            cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ManagementDashboardModel>(context.Errors);
        }

        var dashboard = await _uow.PortalDashboards.GetManagementDashboardAsync(
            context.Value.ManagementCompanyId,
            context.Value.CompanySlug,
            context.Value.CompanyName,
            context.Value.RoleCode ?? string.Empty,
            BuildOptions(),
            cancellationToken);

        return Result.Ok(PortalDashboardMapper.Map(dashboard));
    }

    public async Task<Result<CustomerDashboardModel>> GetCustomerDashboardAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await _portalContext.ResolveCustomerWorkspaceAsync(
            route,
            ManagementAreaRoleCodes,
            allowCustomerContext: true,
            cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<CustomerDashboardModel>(context.Errors);
        }

        var dashboard = await _uow.PortalDashboards.GetCustomerDashboardAsync(
            context.Value.ManagementCompanyId,
            context.Value.CustomerId,
            BuildOptions(),
            cancellationToken);

        return Result.Ok(PortalDashboardMapper.Map(dashboard));
    }

    public async Task<Result<PropertyDashboardModel>> GetPropertyDashboardAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await _portalContext.ResolvePropertyWorkspaceAsync(
            route,
            ManagementAreaRoleCodes,
            allowCustomerContext: true,
            cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<PropertyDashboardModel>(context.Errors);
        }

        var dashboard = await _uow.PortalDashboards.GetPropertyDashboardAsync(
            context.Value.ManagementCompanyId,
            context.Value.CustomerId,
            context.Value.PropertyId,
            BuildOptions(),
            cancellationToken);

        return Result.Ok(PortalDashboardMapper.Map(dashboard));
    }

    public async Task<Result<UnitDashboardModel>> GetUnitDashboardAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await _portalContext.ResolveUnitWorkspaceAsync(route, cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<UnitDashboardModel>(context.Errors);
        }

        var dashboard = await _uow.PortalDashboards.GetUnitDashboardAsync(
            context.Value.ManagementCompanyId,
            context.Value.PropertyId,
            context.Value.UnitId,
            BuildOptions(),
            cancellationToken);

        return Result.Ok(PortalDashboardMapper.Map(dashboard));
    }

    public async Task<Result<ResidentDashboardModel>> GetResidentDashboardAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default)
    {
        var context = await _portalContext.ResolveResidentWorkspaceAsync(
            route,
            ManagementAreaRoleCodes,
            allowResidentContext: true,
            cancellationToken);
        if (context.IsFailed)
        {
            return Result.Fail<ResidentDashboardModel>(context.Errors);
        }

        var dashboard = await _uow.PortalDashboards.GetResidentDashboardAsync(
            context.Value.ManagementCompanyId,
            context.Value.ResidentId,
            BuildOptions(),
            cancellationToken);

        return Result.Ok(PortalDashboardMapper.Map(dashboard));
    }

    private static PortalDashboardQueryOptionsDalDto BuildOptions()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        return new PortalDashboardQueryOptionsDalDto
        {
            UtcNow = now,
            TodayStartUtc = today,
            TomorrowStartUtc = today.AddDays(1),
            NextSevenDaysEndUtc = today.AddDays(8),
            RecentSinceUtc = now.AddDays(-30),
            TodayDate = DateOnly.FromDateTime(now),
            PreviewLimit = PreviewLimit,
            OpenTicketExcludedStatusCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CLOSED" },
            HighPriorityCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "HIGH", "URGENT" },
            CompletedOrCancelledWorkStatusCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "DONE", "CANCELLED" }
        };
    }
}
