using App.BLL.Contracts.Admin;
using App.BLL.DTO.Admin.Dashboard;
using App.DAL.Contracts;

namespace App.BLL.Services.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IAppUOW _uow;

    public AdminDashboardService(IAppUOW uow)
    {
        _uow = uow;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var dto = await _uow.AdminDashboard.GetDashboardAsync(cancellationToken);
        return new AdminDashboardDto
        {
            Stats = new AdminDashboardStatsDto
            {
                TotalUsers = dto.Stats.TotalUsers,
                LockedUsers = dto.Stats.LockedUsers,
                TotalManagementCompanies = dto.Stats.TotalManagementCompanies,
                PendingJoinRequests = dto.Stats.PendingJoinRequests,
                OpenTickets = dto.Stats.OpenTickets,
                OverdueTickets = dto.Stats.OverdueTickets,
                ScheduledWorkToday = dto.Stats.ScheduledWorkToday
            },
            RecentUsers = dto.RecentUsers.Select(user => new AdminRecentUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt
            }).ToList(),
            RecentCompanies = dto.RecentCompanies.Select(company => new AdminRecentCompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                RegistryCode = company.RegistryCode,
                CreatedAt = company.CreatedAt
            }).ToList()
        };
    }
}
