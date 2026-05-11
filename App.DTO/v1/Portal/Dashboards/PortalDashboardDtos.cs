namespace App.DTO.v1.Portal.Dashboards;

public class ManagementDashboardDto
{
    public ManagementDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDto> SummaryMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> WorkMetrics { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> RecentCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityDto> RecentActivity { get; set; } = [];
}

public class CustomerDashboardDto
{
    public CustomerDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDto> PortfolioMetrics { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByProperty { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardRecentActivityDto> RecentActivity { get; set; } = [];
}

public class ResidentDashboardDto
{
    public ResidentDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardLeasePreviewDto> ActiveLeases { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public DashboardContactSummaryDto ContactSummary { get; set; } = new();
    public IReadOnlyList<DashboardRepresentativePreviewDto> Representations { get; set; } = [];
}

public class PropertyDashboardDto
{
    public PropertyDashboardContextDto Context { get; set; } = new();
    public IReadOnlyList<DashboardMetricDto> UnitMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> UnitsByFloor { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByCategory { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardMetricDto> ResidentLeaseMetrics { get; set; } = [];
    public IReadOnlyList<DashboardLeasePreviewDto> CurrentLeases { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardUnitPreviewDto> UnitPreview { get; set; } = [];
}

public class UnitDashboardDto
{
    public UnitDashboardContextDto Context { get; set; } = new();
    public DashboardLeasePreviewDto? CurrentLease { get; set; }
    public IReadOnlyList<DashboardMetricDto> TicketMetrics { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByStatus { get; set; } = [];
    public IReadOnlyList<DashboardBreakdownItemDto> TicketsByPriority { get; set; } = [];
    public IReadOnlyList<DashboardTicketPreviewDto> RecentTickets { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> UpcomingWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> DelayedWork { get; set; } = [];
    public IReadOnlyList<DashboardWorkPreviewDto> RecentlyCompletedWork { get; set; } = [];
    public IReadOnlyList<DashboardTimelineItemDto> Timeline { get; set; } = [];
}

public class ManagementDashboardContextDto
{
    public Guid ManagementCompanyId { get; set; }
    public string CompanySlug { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ApiPath { get; set; } = string.Empty;
}

public class CustomerDashboardContextDto : ManagementDashboardContextDto
{
    public Guid CustomerId { get; set; }
    public string CustomerSlug { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RegistryCode { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? Phone { get; set; }
}

public class ResidentDashboardContextDto : ManagementDashboardContextDto
{
    public Guid ResidentId { get; set; }
    public string ResidentIdCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PreferredLanguage { get; set; }
}

public class PropertyDashboardContextDto : CustomerDashboardContextDto
{
    public Guid PropertyId { get; set; }
    public string PropertySlug { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyTypeLabel { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

public class UnitDashboardContextDto : PropertyDashboardContextDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
}

public class DashboardMetricDto
{
    public string Key { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardBreakdownItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardTicketPreviewDto
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
    public string Path { get; set; } = string.Empty;
    public string EditPath { get; set; } = string.Empty;
}

public class DashboardWorkPreviewDto
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
    public string Path { get; set; } = string.Empty;
    public string EditPath { get; set; } = string.Empty;
    public string TicketPath { get; set; } = string.Empty;
}

public class DashboardRecentActivityDto
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
    public string Path { get; set; } = string.Empty;
}

public class DashboardRepresentativePreviewDto
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

public class DashboardLeasePreviewDto
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
    public string Path { get; set; } = string.Empty;
}

public class DashboardContactSummaryDto
{
    public DashboardContactPreviewDto? PrimaryContact { get; set; }
    public IReadOnlyList<DashboardBreakdownItemDto> ContactMethodCounts { get; set; } = [];
}

public class DashboardContactPreviewDto
{
    public Guid ContactId { get; set; }
    public string ContactTypeCode { get; set; } = string.Empty;
    public string ContactTypeLabel { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
}

public class DashboardUnitPreviewDto
{
    public Guid UnitId { get; set; }
    public string UnitSlug { get; set; } = string.Empty;
    public string UnitNr { get; set; } = string.Empty;
    public int? FloorNr { get; set; }
    public decimal? SizeM2 { get; set; }
    public bool HasActiveLease { get; set; }
    public string? CurrentResidentName { get; set; }
    public int OpenTicketCount { get; set; }
    public string Path { get; set; } = string.Empty;
    public string EditPath { get; set; } = string.Empty;
}

public class DashboardTimelineItemDto
{
    public string ItemType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? SupportingText { get; set; }
    public DateTime EventAt { get; set; }
    public Guid? TicketId { get; set; }
    public Guid? ScheduledWorkId { get; set; }
    public Guid? LeaseId { get; set; }
    public string Path { get; set; } = string.Empty;
}
