namespace App.BLL.DTO.Admin.Dashboard;

public class AdminDashboardDto
{
    public AdminDashboardStatsDto Stats { get; set; } = new();
    public IReadOnlyList<AdminRecentUserDto> RecentUsers { get; set; } = [];
    public IReadOnlyList<AdminRecentCompanyDto> RecentCompanies { get; set; } = [];
}

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int LockedUsers { get; set; }
    public int TotalManagementCompanies { get; set; }
    public int PendingJoinRequests { get; set; }
    public int OpenTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int ScheduledWorkToday { get; set; }
}

public class AdminRecentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminRecentCompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
