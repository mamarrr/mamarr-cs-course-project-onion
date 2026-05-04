namespace App.BLL.Contracts.Common.Deletion;

public interface IAppDeleteOrchestrator
{
    Task<bool> DeleteUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePropertyAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteCustomerAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default);
}
