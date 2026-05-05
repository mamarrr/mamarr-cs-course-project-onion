namespace App.BLL.Contracts.Common.Deletion;

public interface IAppDeleteGuard
{
    Task<bool> CanDeleteUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> CanDeleteTicketAsync(
        Guid ticketId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> CanDeletePropertyAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> CanDeleteCustomerAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> CanDeleteResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
