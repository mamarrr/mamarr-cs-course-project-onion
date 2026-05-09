namespace WebApp.ViewModels.Dashboards;

public class ManagementDashboardViewModel
{
    public string CompanyName { get; init; } = string.Empty;
    public string RoleCode { get; init; } = string.Empty;
    public IReadOnlyList<DashboardMetricViewModel> SummaryMetrics { get; init; } = [];
    public IReadOnlyList<DashboardMetricViewModel> TicketMetrics { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByStatus { get; init; } = [];
    public IReadOnlyList<DashboardTicketPreviewViewModel> RecentTickets { get; init; } = [];
    public IReadOnlyList<DashboardMetricViewModel> WorkMetrics { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> RecentCompletedWork { get; init; } = [];
    public IReadOnlyList<DashboardActivityViewModel> RecentActivity { get; init; } = [];
}

public class CustomerDashboardViewModel
{
    public string CustomerName { get; init; } = string.Empty;
    public string RegistryCode { get; init; } = string.Empty;
    public string? BillingEmail { get; init; }
    public string? Phone { get; init; }
    public IReadOnlyList<DashboardMetricViewModel> PortfolioMetrics { get; init; } = [];
    public IReadOnlyList<DashboardMetricViewModel> TicketMetrics { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByProperty { get; init; } = [];
    public IReadOnlyList<DashboardTicketPreviewViewModel> RecentTickets { get; init; } = [];
    public IReadOnlyList<DashboardActivityViewModel> RecentActivity { get; init; } = [];
}

public class ResidentDashboardViewModel
{
    public string FullName { get; init; } = string.Empty;
    public string IdCode { get; init; } = string.Empty;
    public string? PreferredLanguage { get; init; }
    public IReadOnlyList<DashboardLeaseViewModel> ActiveLeases { get; init; } = [];
    public IReadOnlyList<DashboardMetricViewModel> TicketMetrics { get; init; } = [];
    public IReadOnlyList<DashboardTicketPreviewViewModel> RecentTickets { get; init; } = [];
    public DashboardContactSummaryViewModel ContactSummary { get; init; } = new();
    public IReadOnlyList<DashboardRepresentativeViewModel> Representations { get; init; } = [];
}

public class PropertyDashboardViewModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string PropertyTypeLabel { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public IReadOnlyList<DashboardMetricViewModel> UnitMetrics { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> UnitsByFloor { get; init; } = [];
    public IReadOnlyList<DashboardMetricViewModel> TicketMetrics { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByStatus { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByPriority { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByCategory { get; init; } = [];
    public IReadOnlyList<DashboardTicketPreviewViewModel> RecentTickets { get; init; } = [];
    public IReadOnlyList<DashboardMetricViewModel> ResidentLeaseMetrics { get; init; } = [];
    public IReadOnlyList<DashboardLeaseViewModel> CurrentLeases { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> UpcomingWork { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> DelayedWork { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> RecentlyCompletedWork { get; init; } = [];
    public IReadOnlyList<DashboardUnitPreviewViewModel> UnitPreview { get; init; } = [];
}

public class UnitDashboardViewModel
{
    public string UnitNr { get; init; } = string.Empty;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public DashboardLeaseViewModel? CurrentLease { get; init; }
    public IReadOnlyList<DashboardMetricViewModel> TicketMetrics { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByStatus { get; init; } = [];
    public IReadOnlyList<DashboardBreakdownViewModel> TicketsByPriority { get; init; } = [];
    public IReadOnlyList<DashboardTicketPreviewViewModel> RecentTickets { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> UpcomingWork { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> DelayedWork { get; init; } = [];
    public IReadOnlyList<DashboardWorkPreviewViewModel> RecentlyCompletedWork { get; init; } = [];
    public IReadOnlyList<DashboardTimelineItemViewModel> Timeline { get; init; } = [];
}

public class DashboardMetricViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
}

public class DashboardBreakdownViewModel
{
    public string Code { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class DashboardTicketPreviewViewModel
{
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string PriorityLabel { get; init; } = string.Empty;
    public DateTime? DueAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitSlug { get; init; }
    public string? ResidentIdCode { get; init; }
}

public class DashboardWorkPreviewViewModel
{
    public Guid ScheduledWorkId { get; init; }
    public Guid TicketId { get; init; }
    public string TicketNr { get; init; } = string.Empty;
    public string TicketTitle { get; init; } = string.Empty;
    public string VendorName { get; init; } = string.Empty;
    public string WorkStatusCode { get; init; } = string.Empty;
    public string WorkStatusLabel { get; init; } = string.Empty;
    public DateTime ScheduledStart { get; init; }
    public DateTime? ScheduledEnd { get; init; }
    public DateTime? RealEnd { get; init; }
}

public class DashboardActivityViewModel
{
    public string ItemType { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? SupportingText { get; init; }
    public DateTime EventAt { get; init; }
    public string? CustomerSlug { get; init; }
    public string? PropertySlug { get; init; }
    public string? UnitSlug { get; init; }
    public string? ResidentIdCode { get; init; }
    public Guid? TicketId { get; init; }
}

public class DashboardRepresentativeViewModel
{
    public string ResidentIdCode { get; init; } = string.Empty;
    public string ResidentName { get; init; } = string.Empty;
    public string RoleLabel { get; init; } = string.Empty;
    public DateOnly ValidFrom { get; init; }
    public DateOnly? ValidTo { get; init; }
    public string? CustomerSlug { get; init; }
    public string? CustomerName { get; init; }
}

public class DashboardLeaseViewModel
{
    public Guid LeaseId { get; init; }
    public string CustomerSlug { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string PropertySlug { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string UnitSlug { get; init; } = string.Empty;
    public string UnitNr { get; init; } = string.Empty;
    public string ResidentIdCode { get; init; } = string.Empty;
    public string ResidentName { get; init; } = string.Empty;
    public string RoleLabel { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
}

public class DashboardContactSummaryViewModel
{
    public DashboardContactPreviewViewModel? PrimaryContact { get; init; }
    public IReadOnlyList<DashboardBreakdownViewModel> ContactMethodCounts { get; init; } = [];
}

public class DashboardContactPreviewViewModel
{
    public string ContactTypeLabel { get; init; } = string.Empty;
    public string ContactValue { get; init; } = string.Empty;
}

public class DashboardUnitPreviewViewModel
{
    public string UnitSlug { get; init; } = string.Empty;
    public string UnitNr { get; init; } = string.Empty;
    public int? FloorNr { get; init; }
    public decimal? SizeM2 { get; init; }
    public bool HasActiveLease { get; init; }
    public string? CurrentResidentName { get; init; }
    public int OpenTicketCount { get; init; }
}

public class DashboardTimelineItemViewModel
{
    public string ItemType { get; init; } = string.Empty;
    public string TypeLabel { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? SupportingText { get; init; }
    public DateTime EventAt { get; init; }
    public Guid? TicketId { get; init; }
    public Guid? ScheduledWorkId { get; init; }
    public Guid? LeaseId { get; init; }
}
