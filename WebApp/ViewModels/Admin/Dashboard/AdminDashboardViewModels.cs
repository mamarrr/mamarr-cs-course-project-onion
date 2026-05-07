using WebApp.ViewModels.Admin;

namespace WebApp.ViewModels.Admin.Dashboard;

public class AdminDashboardViewModel : AdminPageViewModel
{
    public AdminDashboardStatsViewModel Stats { get; set; } = new();
    public IReadOnlyList<AdminRecentUserViewModel> RecentUsers { get; set; } = [];
    public IReadOnlyList<AdminRecentCompanyViewModel> RecentCompanies { get; set; } = [];
}

public class AdminDashboardStatsViewModel
{
    public int TotalUsers { get; set; }
    public int LockedUsers { get; set; }
    public int TotalManagementCompanies { get; set; }
    public int PendingJoinRequests { get; set; }
    public int OpenTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int ScheduledWorkToday { get; set; }
}

public class AdminRecentUserViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminRecentCompanyViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
