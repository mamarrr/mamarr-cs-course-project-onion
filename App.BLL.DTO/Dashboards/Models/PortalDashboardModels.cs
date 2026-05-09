namespace App.BLL.DTO.Dashboards.Models;

public class ManagementDashboardModel
{
    public ManagementDashboardContextModel Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricModel> SummaryMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewModel> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> WorkMetrics { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> RecentCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> JoinRequestMetrics { get; set; } = [];
    public IReadOnlyList<DashboardJoinRequestPreviewModel> PendingJoinRequests { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> TeamMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TeamRoleDistribution { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityModel> RecentActivity { get; set; } = [];
}

public class CustomerDashboardModel
{
    public CustomerDashboardContextModel Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricModel> PortfolioMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByProperty { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewModel> RecentTickets { get; set; } = [];
    public int ActiveRepresentativeCount { get; set; }
    public IReadOnlyList<DashboardRepresentativePreviewModel> ActiveRepresentatives { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityModel> RecentActivity { get; set; } = [];
}

public class ResidentDashboardModel
{
    public ResidentDashboardContextModel Context { get; set; } = new();
    public IReadOnlyList<DashboardLeasePreviewModel> ActiveLeases { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewModel> RecentTickets { get; set; } = [];
    public DashboardContactSummaryModel ContactSummary { get; set; } = new();
    public IReadOnlyList<DashboardRepresentativePreviewModel> Representations { get; set; } = [];
}

public class PropertyDashboardModel
{
    public PropertyDashboardContextModel Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricModel> UnitMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> UnitsByFloor { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByCategory { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewModel> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricModel> ResidentLeaseMetrics { get; set; } = [];
    public IReadOnlyList<DashboardLeasePreviewModel> CurrentLeases { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardUnitPreviewModel> UnitPreview { get; set; } = [];
}

public class UnitDashboardModel
{
    public UnitDashboardContextModel Context { get; set; } = new();
    public DashboardLeasePreviewModel? CurrentLease { get; set; }
    public IReadOnlyList<DashboardMetricModel> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemModel> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewModel> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewModel> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardTimelineItemModel> Timeline { get; set; } = [];
}

public class ManagementDashboardContextModel
{
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class CustomerDashboardContextModel : ManagementDashboardContextModel
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? Phone { get; set; }
}

public class ResidentDashboardContextModel : ManagementDashboardContextModel
{
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
}

public class PropertyDashboardContextModel : CustomerDashboardContextModel
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class UnitDashboardContextModel : PropertyDashboardContextModel
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
}

public class DashboardMetricModel
{
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardBreakdownItemModel
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardTicketPreviewModel
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

public class DashboardWorkPreviewModel
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

public class DashboardRecentActivityModel
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

public class DashboardJoinRequestPreviewModel
{
    public Guid JoinRequestId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string RequestedRoleCode { get; set; } = string.Empty;
    public string RequestedRoleLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DashboardRepresentativePreviewModel
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

public class DashboardLeasePreviewModel
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

public class DashboardContactSummaryModel
{
    public DashboardContactPreviewModel? PrimaryContact { get; set; }
    public IReadOnlyList<DashboardBreakdownItemModel> ContactMethodCounts { get; set; } = [];
}

public class DashboardContactPreviewModel
{
    public Guid ContactId { get; set; }
    public string ContactTypeCode { get; set; } = string.Empty;
    public string ContactTypeLabel { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
}

public class DashboardUnitPreviewModel
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

public class DashboardTimelineItemModel
{
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SupportingText { get; set; }
    public DateTime EventAt { get; set; }
    public Guid? TicketId { get; set; }
    public Guid? ScheduledWorkId { get; set; }
    public Guid? LeaseId { get; set; }
}
