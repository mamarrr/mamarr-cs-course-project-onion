using App.DAL.EF;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Shared.Deletion;

internal static class ProfileDeleteOrchestrator
{
    public static async Task DeleteTicketsAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        if (ticketIds.Count == 0)
        {
            return;
        }

        var scheduledWorkIds = await dbContext.ScheduledWorks
            .Where(sw => ticketIds.Contains(sw.TicketId))
            .Select(sw => sw.Id)
            .ToListAsync(cancellationToken);

        if (scheduledWorkIds.Count > 0)
        {
            await dbContext.WorkLogs
                .Where(wl => scheduledWorkIds.Contains(wl.ScheduledWorkId))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.ScheduledWorks
                .Where(sw => scheduledWorkIds.Contains(sw.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await dbContext.Tickets
            .Where(t => ticketIds.Contains(t.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public static async Task DeleteContactsIfOrphanedAsync(
        AppDbContext dbContext,
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken)
    {
        if (contactIds.Count == 0)
        {
            return;
        }

        var orphanedContactIds = await dbContext.Contacts
            .Where(c => contactIds.Contains(c.Id))
            .Where(c => !dbContext.ResidentContacts.Any(rc => rc.ContactId == c.Id))
            .Where(c => !dbContext.VendorContacts.Any(vc => vc.ContactId == c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (orphanedContactIds.Count == 0)
        {
            return;
        }

        await dbContext.Contacts
            .Where(c => orphanedContactIds.Contains(c.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}

