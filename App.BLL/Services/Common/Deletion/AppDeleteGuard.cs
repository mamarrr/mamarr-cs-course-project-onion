using App.BLL.Contracts.Common.Deletion;
using App.DAL.Contracts;

namespace App.BLL.Services.Common.Deletion;

internal sealed class AppDeleteGuard : IAppDeleteGuard
{
    private readonly IAppUOW _uow;

    public AppDeleteGuard(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<bool> CanDeleteUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return !await _uow.Units.HasDeleteDependenciesAsync(
            unitId,
            propertyId,
            managementCompanyId,
            cancellationToken);
    }

    public async Task<bool> CanDeleteTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return !await _uow.Tickets.HasDeleteDependenciesAsync(
            ticketId,
            managementCompanyId,
            cancellationToken);
    }

    public async Task<bool> CanDeletePropertyAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return !await _uow.Properties.HasDeleteDependenciesAsync(
            propertyId,
            customerId,
            managementCompanyId,
            cancellationToken);
    }

    public async Task<bool> CanDeleteCustomerAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return !await _uow.Customers.HasDeleteDependenciesAsync(
            customerId,
            managementCompanyId,
            cancellationToken);
    }

    public async Task<bool> CanDeleteResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return !await _uow.Residents.HasDeleteDependenciesAsync(
            residentId,
            managementCompanyId,
            cancellationToken);
    }
}
