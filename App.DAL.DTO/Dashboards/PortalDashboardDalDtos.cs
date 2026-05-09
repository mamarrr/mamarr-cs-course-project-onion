namespace App.DAL.DTO.Dashboards;

public class PortalDashboardQueryOptionsDalDto
{
    public DateTime UtcNow { get; set; }
    public DateTime TodayStartUtc { get; set; }
    public DateTime TomorrowStartUtc { get; set; }
    public DateTime NextSevenDaysEndUtc { get; set; }
    public DateTime RecentSinceUtc { get; set; }
    public DateOnly TodayDate { get; set; }
    public int PreviewLimit { get; set; }
    public IReadOnlySet<string> OpenTicketExcludedStatusCodes { get; set; } = new HashSet<string>();
    public IReadOnlySet<string> HighPriorityCodes { get; set; } = new HashSet<string>();
    public IReadOnlySet<string> CompletedOrCancelledWorkStatusCodes { get; set; } = new HashSet<string>();
}

public class ManagementDashboardDalDto
{
    public ManagementDashboardContextDalDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDalDto> SummaryMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDalDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> WorkMetrics { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> RecentCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> JoinRequestMetrics { get; set; } = [];
    public IReadOnlyList<DashboardJoinRequestPreviewDalDto> PendingJoinRequests { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> TeamMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TeamRoleDistribution { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityDalDto> RecentActivity { get; set; } = [];
}

public class CustomerDashboardDalDto
{
    public CustomerDashboardContextDalDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDalDto> PortfolioMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByProperty { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDalDto> RecentTickets { get; set; } = [];
    public int ActiveRepresentativeCount { get; set; }
    public IReadOnlyList<DashboardRepresentativePreviewDalDto> ActiveRepresentatives { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityDalDto> RecentActivity { get; set; } = [];
}

public class ResidentDashboardDalDto
{
    public ResidentDashboardContextDalDto Context { get; set; } = new();
    public IReadOnlyList<DashboardLeasePreviewDalDto> ActiveLeases { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDalDto> RecentTickets { get; set; } = [];
    public DashboardContactSummaryDalDto ContactSummary { get; set; } = new();
    public IReadOnlyList<DashboardRepresentativePreviewDalDto> Representations { get; set; } = [];
}

public class PropertyDashboardDalDto
{
    public PropertyDashboardContextDalDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDalDto> UnitMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> UnitsByFloor { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByCategory { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDalDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricDalDto> ResidentLeaseMetrics { get; set; } = [];
    public IReadOnlyList<DashboardLeasePreviewDalDto> CurrentLeases { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardUnitPreviewDalDto> UnitPreview { get; set; } = [];
}

public class UnitDashboardDalDto
{
    public UnitDashboardContextDalDto Context { get; set; } = new();
    public DashboardLeasePreviewDalDto? CurrentLease { get; set; }
    public IReadOnlyList<DashboardMetricDalDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDalDto> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDalDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDalDto> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardTimelineItemDalDto> Timeline { get; set; } = [];
}

public class ManagementDashboardContextDalDto
{
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class CustomerDashboardContextDalDto : ManagementDashboardContextDalDto
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? Phone { get; set; }
}

public class ResidentDashboardContextDalDto : ManagementDashboardContextDalDto
{
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
}

public class PropertyDashboardContextDalDto : CustomerDashboardContextDalDto
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class UnitDashboardContextDalDto : PropertyDashboardContextDalDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
}

public class DashboardMetricDalDto
{
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardBreakdownItemDalDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardTicketPreviewDalDto
{
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PriorityCode { get; set; } = string.Empty;
    public string PriorityLabel { get; set; } = string.Empty;
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
    public string? PropertySlug { get; set; }
    public string? PropertyName { get; set; }
    public string? UnitSlug { get; set; }
    public string? UnitNr { get; set; }
    public string? ResidentIdCode { get; set; }
    public string? ResidentName { get; set; }
}

public class DashboardWorkPreviewDalDto
{
    public Guid ScheduledWorkId { get; set; }
    public Guid TicketId { get; set; }
    public string TicketNr { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string WorkStatusCode { get; set; } = string.Empty;
    public string WorkStatusLabel { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public DateTime? RealStart { get; set; }
    public DateTime? RealEnd { get; set; }
}

public class DashboardRecentActivityDalDto
{
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SupportingText { get; set; }
    public DateTime EventAt { get; set; }
    public string? CustomerSlug { get; set; }
    public string? PropertySlug { get; set; }
    public string? UnitSlug { get; set; }
    public string? ResidentIdCode { get; set; }
    public Guid? TicketId { get; set; }
}

public class DashboardJoinRequestPreviewDalDto
{
    public Guid JoinRequestId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string RequestedRoleCode { get; set; } = string.Empty;
    public string RequestedRoleLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DashboardRepresentativePreviewDalDto
{
    public Guid RepresentativeId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? CustomerSlug { get; set; }
    public string? CustomerName { get; set; }
}

public class DashboardLeasePreviewDalDto
{
    public Guid LeaseId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public string ResidentIdCode { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardContactSummaryDalDto
{
    public DashboardContactPreviewDalDto? PrimaryContact { get; set; }
    public IReadOnlyList<DashboardBreakdownItemDalDto> ContactMethodCounts { get; set; } = [];
}

public class DashboardContactPreviewDalDto
{
    public Guid ContactId { get; set; }
    public string ContactTypeCode { get; set; } = string.Empty;
    public string ContactTypeLabel { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
}

public class DashboardUnitPreviewDalDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public bool HasActiveLease { get; set; }
    public string? CurrentResidentName { get; set; }
    public int OpenTicketCount { get; set; }
}

public class DashboardTimelineItemDalDto
{
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SupportingText { get; set; }
    public DateTime EventAt { get; set; }
    public Guid? TicketId { get; set; }
    public Guid? ScheduledWorkId { get; set; }
    public Guid? LeaseId { get; set; }
}
