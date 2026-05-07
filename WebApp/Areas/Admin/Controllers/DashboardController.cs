using App.BLL.Contracts;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Admin.Dashboard;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SystemAdmin")]
[Route("Admin")]
public class DashboardController : Controller
{
    private readonly IAppBLL _bll;

    public DashboardController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("")]
    [HttpGet("Dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminDashboard.GetDashboardAsync(cancellationToken);
        return View(new AdminDashboardViewModel
        {
            PageTitle = AdminText.AdminDashboard,
            ActiveSection = "Dashboard",
            Stats = new AdminDashboardStatsViewModel
            {
                TotalUsers = dto.Stats.TotalUsers,
                LockedUsers = dto.Stats.LockedUsers,
                TotalManagementCompanies = dto.Stats.TotalManagementCompanies,
                PendingJoinRequests = dto.Stats.PendingJoinRequests,
                OpenTickets = dto.Stats.OpenTickets,
                OverdueTickets = dto.Stats.OverdueTickets,
                ScheduledWorkToday = dto.Stats.ScheduledWorkToday
            },
            RecentUsers = dto.RecentUsers.Select(user => new AdminRecentUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt
            }).ToList(),
            RecentCompanies = dto.RecentCompanies.Select(company => new AdminRecentCompanyViewModel
            {
                Id = company.Id,
                Name = company.Name,
                RegistryCode = company.RegistryCode,
                CreatedAt = company.CreatedAt
            }).ToList()
        });
    }
}
