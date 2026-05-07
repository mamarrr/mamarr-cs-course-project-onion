namespace App.DAL.DTO.Admin.Dashboard;

public class AdminDashboardDalDto
{
    public AdminDashboardStatsDalDto Stats { get; set; } = new();
    public IReadOnlyList<AdminRecentUserDalDto> RecentUsers { get; set; } = [];
    public IReadOnlyList<AdminRecentCompanyDalDto> RecentCompanies { get; set; } = [];
}

public class AdminDashboardStatsDalDto
{
    public int TotalUsers { get; set; }
    public int LockedUsers { get; set; }
    public int TotalManagementCompanies { get; set; }
    public int PendingJoinRequests { get; set; }
    public int OpenTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int ScheduledWorkToday { get; set; }
}

public class AdminRecentUserDalDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminRecentCompanyDalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
