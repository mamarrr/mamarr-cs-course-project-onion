using App.DAL.DTO.Dashboards;

namespace App.DAL.Contracts.Repositories.Dashboards;

public interface IPortalDashboardRepository
{
    Task<ManagementDashboardDalDto> GetManagementDashboardAsync(
        Guid managementCompanyId,
        string companySlug,
        string companyName,
        string roleCode,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default);

    Task<CustomerDashboardDalDto> GetCustomerDashboardAsync(
        Guid managementCompanyId,
        Guid customerId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default);

    Task<PropertyDashboardDalDto> GetPropertyDashboardAsync(
        Guid managementCompanyId,
        Guid customerId,
        Guid propertyId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default);

    Task<UnitDashboardDalDto> GetUnitDashboardAsync(
        Guid managementCompanyId,
        Guid propertyId,
        Guid unitId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default);

    Task<ResidentDashboardDalDto> GetResidentDashboardAsync(
        Guid managementCompanyId,
        Guid residentId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default);
}
