using App.BLL.DTO.Common.Routes;
using App.BLL.DTO.Dashboards.Models;
using FluentResults;

namespace App.BLL.Contracts.Dashboards;

public interface IPortalDashboardService
{
    Task<Result<ManagementDashboardModel>> GetManagementDashboardAsync(
        ManagementCompanyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<CustomerDashboardModel>> GetCustomerDashboardAsync(
        CustomerRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<PropertyDashboardModel>> GetPropertyDashboardAsync(
        PropertyRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<UnitDashboardModel>> GetUnitDashboardAsync(
        UnitRoute route,
        CancellationToken cancellationToken = default);

    Task<Result<ResidentDashboardModel>> GetResidentDashboardAsync(
        ResidentRoute route,
        CancellationToken cancellationToken = default);
}
