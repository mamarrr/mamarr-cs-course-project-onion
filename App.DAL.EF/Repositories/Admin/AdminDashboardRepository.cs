using App.DAL.Contracts.Repositories.Admin;
using App.DAL.DTO.Admin.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories.Admin;

public class AdminDashboardRepository : IAdminDashboardRepository
{
    private readonly AppDbContext _dbContext;

    public AdminDashboardRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardDalDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var pendingStatusIds = await _dbContext.ManagementCompanyJoinRequestStatuses
            .AsNoTracking()
            .Where(status => status.Code == "PENDING")
            .Select(status => status.Id)
            .ToListAsync(cancellationToken);

        var closedStatusIds = await _dbContext.TicketStatuses
            .AsNoTracking()
            .Where(status => status.Code == "CLOSED")
            .Select(status => status.Id)
            .ToListAsync(cancellationToken);

        return new AdminDashboardDalDto
        {
            Stats = new AdminDashboardStatsDalDto
            {
                TotalUsers = await _dbContext.Users.CountAsync(cancellationToken),
                LockedUsers = await _dbContext.Users.CountAsync(user => user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow, cancellationToken),
                TotalManagementCompanies = await _dbContext.ManagementCompanies.CountAsync(cancellationToken),
                PendingJoinRequests = await _dbContext.ManagementCompanyJoinRequests.CountAsync(request => pendingStatusIds.Contains(request.ManagementCompanyJoinRequestStatusId), cancellationToken),
                OpenTickets = await _dbContext.Tickets.CountAsync(ticket => ticket.ClosedAt == null && !closedStatusIds.Contains(ticket.TicketStatusId), cancellationToken),
                OverdueTickets = await _dbContext.Tickets.CountAsync(ticket => ticket.DueAt != null && ticket.DueAt < now && ticket.ClosedAt == null, cancellationToken),
                ScheduledWorkToday = await _dbContext.ScheduledWorks.CountAsync(work => work.ScheduledStart >= today && work.ScheduledStart < tomorrow, cancellationToken)
            },
            RecentUsers = await _dbContext.Users
                .AsNoTracking()
                .OrderByDescending(user => user.CreatedAt)
                .Take(6)
                .Select(user => new AdminRecentUserDalDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = (user.FirstName + " " + user.LastName).Trim(),
                    CreatedAt = user.CreatedAt
                })
                .ToListAsync(cancellationToken),
            RecentCompanies = await _dbContext.ManagementCompanies
                .AsNoTracking()
                .OrderByDescending(company => company.CreatedAt)
                .Take(6)
                .Select(company => new AdminRecentCompanyDalDto
                {
                    Id = company.Id,
                    Name = company.Name,
                    RegistryCode = company.RegistryCode,
                    CreatedAt = company.CreatedAt
                })
                .ToListAsync(cancellationToken)
        };
    }
}
