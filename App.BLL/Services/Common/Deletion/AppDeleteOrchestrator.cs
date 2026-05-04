using App.BLL.Contracts.Common.Deletion;
using App.DAL.Contracts;

namespace App.BLL.Services.Common.Deletion;

public class AppDeleteOrchestrator : IAppDeleteOrchestrator
{
    private readonly IAppUOW _uow;

    public AppDeleteOrchestrator(IAppUOW uow)
    {
        _uow = uow;
    }

    public Task<bool> DeleteUnitAsync(
        Guid unitId,
        Guid propertyId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            var exists = await _uow.Units.ExistsInCompanyAsync(unitId, managementCompanyId, cancellationToken);
            if (!exists)
            {
                return false;
            }

            var ticketIds = await _uow.Tickets.AllIdsForUnitScopeAsync(
                unitId,
                managementCompanyId,
                cancellationToken);

            await DeleteTicketsAsync(ticketIds, managementCompanyId, cancellationToken);
            await _uow.Leases.DeleteByUnitIdsAsync([unitId], cancellationToken);

            var deleted = await _uow.Units.DeleteAsync(
                unitId,
                propertyId,
                managementCompanyId,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return deleted;
        }, cancellationToken);
    }

    public Task<bool> DeletePropertyAsync(
        Guid propertyId,
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            var exists = await _uow.Properties.ExistsInCompanyAsync(
                propertyId,
                managementCompanyId,
                cancellationToken);
            if (!exists)
            {
                return false;
            }

            var unitIds = await _uow.Units.AllIdsByPropertyIdsAsync([propertyId], cancellationToken);
            var ticketIds = await _uow.Tickets.AllIdsForPropertyScopeAsync(
                propertyId,
                unitIds,
                managementCompanyId,
                cancellationToken);

            await DeleteTicketsAsync(ticketIds, managementCompanyId, cancellationToken);
            await _uow.Leases.DeleteByUnitIdsAsync(unitIds, cancellationToken);
            await _uow.Units.DeleteByIdsAsync(unitIds, cancellationToken);

            var deleted = await _uow.Properties.DeleteAsync(
                propertyId,
                customerId,
                managementCompanyId,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return deleted;
        }, cancellationToken);
    }

    public Task<bool> DeleteCustomerAsync(
        Guid customerId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            var exists = await _uow.Customers.ExistsInCompanyAsync(
                customerId,
                managementCompanyId,
                cancellationToken);
            if (!exists)
            {
                return false;
            }

            var propertyIds = await _uow.Properties.AllIdsByCustomerIdAsync(customerId, cancellationToken);
            var unitIds = await _uow.Units.AllIdsByPropertyIdsAsync(propertyIds, cancellationToken);
            var ticketIds = await _uow.Tickets.AllIdsForCustomerScopeAsync(
                customerId,
                propertyIds,
                unitIds,
                managementCompanyId,
                cancellationToken);

            await DeleteTicketsAsync(ticketIds, managementCompanyId, cancellationToken);
            await _uow.Customers.DeleteRepresentativesByCustomerIdAsync(customerId, cancellationToken);
            await _uow.Leases.DeleteByUnitIdsAsync(unitIds, cancellationToken);
            await _uow.Units.DeleteByIdsAsync(unitIds, cancellationToken);
            await _uow.Properties.DeleteByIdsAsync(propertyIds, cancellationToken);

            var deleted = await _uow.Customers.DeleteAsync(
                customerId,
                managementCompanyId,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return deleted;
        }, cancellationToken);
    }

    public Task<bool> DeleteResidentAsync(
        Guid residentId,
        Guid managementCompanyId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteInTransactionAsync(async () =>
        {
            var exists = await _uow.Residents.ExistsInCompanyAsync(
                residentId,
                managementCompanyId,
                cancellationToken);
            if (!exists)
            {
                return false;
            }

            var ticketIds = await _uow.Tickets.AllIdsForResidentScopeAsync(
                residentId,
                managementCompanyId,
                cancellationToken);
            var contactIds = await _uow.Contacts.AllIdsByResidentIdAsync(residentId, cancellationToken);

            await DeleteTicketsAsync(ticketIds, managementCompanyId, cancellationToken);
            await _uow.Customers.DeleteRepresentativesByResidentIdAsync(residentId, cancellationToken);
            await _uow.Leases.DeleteByResidentIdAsync(residentId, cancellationToken);
            await _uow.Residents.DeleteUsersByResidentIdAsync(residentId, cancellationToken);
            await _uow.Contacts.DeleteResidentLinksByResidentIdAsync(residentId, cancellationToken);
            await _uow.Contacts.DeleteOrphanedByIdsAsync(contactIds, cancellationToken);

            var deleted = await _uow.Residents.DeleteAsync(
                residentId,
                managementCompanyId,
                cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);
            return deleted;
        }, cancellationToken);
    }

    private async Task DeleteTicketsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        Guid managementCompanyId,
        CancellationToken cancellationToken)
    {
        await _uow.Tickets.DeleteDependentsByTicketIdsAsync(ticketIds, cancellationToken);
        await _uow.Tickets.DeleteByIdsAsync(ticketIds, managementCompanyId, cancellationToken);
    }

    private async Task<bool> ExecuteInTransactionAsync(
        Func<Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            if (!result)
            {
                await _uow.RollbackTransactionAsync(cancellationToken);
                return false;
            }

            await _uow.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await TryRollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task TryRollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // The transaction may already be disposed if commit failed after reaching the database.
        }
    }
}
