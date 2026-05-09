using App.BLL.DTO.Dashboards.Models;
using App.Resources.Views;
using WebApp.ViewModels.Dashboards;

namespace WebApp.Mappers.Dashboards;

public static class PortalDashboardViewModelMapper
{
    public static ManagementDashboardViewModel Map(ManagementDashboardModel model) => new()
    {
        CompanyName = model.Context.CompanyName,
        RoleCode = model.Context.RoleCode,
        SummaryMetrics = MapMetrics(model.SummaryMetrics),
        TicketMetrics = MapMetrics(model.TicketMetrics),
        TicketsByStatus = MapBreakdowns(model.TicketsByStatus),
        RecentTickets = MapTickets(model.RecentTickets),
        WorkMetrics = MapMetrics(model.WorkMetrics),
        RecentCompletedWork = MapWorks(model.RecentCompletedWork),
        JoinRequestMetrics = MapMetrics(model.JoinRequestMetrics),
        PendingJoinRequests = model.PendingJoinRequests.Select(Map).ToList(),
        TeamMetrics = MapMetrics(model.TeamMetrics),
        TeamRoleDistribution = MapBreakdowns(model.TeamRoleDistribution),
        RecentActivity = model.RecentActivity.Select(Map).ToList()
    };

    public static CustomerDashboardViewModel Map(CustomerDashboardModel model) => new()
    {
        CustomerName = model.Context.CustomerName,
        RegistryCode = model.Context.RegistryCode,
        BillingEmail = model.Context.BillingEmail,
        Phone = model.Context.Phone,
        PortfolioMetrics = MapMetrics(model.PortfolioMetrics),
        TicketMetrics = MapMetrics(model.TicketMetrics),
        TicketsByProperty = MapBreakdowns(model.TicketsByProperty),
        RecentTickets = MapTickets(model.RecentTickets),
        ActiveRepresentativeCount = model.ActiveRepresentativeCount,
        ActiveRepresentatives = model.ActiveRepresentatives.Select(Map).ToList(),
        RecentActivity = model.RecentActivity.Select(Map).ToList()
    };

    public static ResidentDashboardViewModel Map(ResidentDashboardModel model) => new()
    {
        FullName = model.Context.FullName,
        IdCode = model.Context.ResidentIdCode,
        PreferredLanguage = model.Context.PreferredLanguage,
        ActiveLeases = model.ActiveLeases.Select(Map).ToList(),
        TicketMetrics = MapMetrics(model.TicketMetrics),
        RecentTickets = MapTickets(model.RecentTickets),
        ContactSummary = Map(model.ContactSummary),
        Representations = model.Representations.Select(Map).ToList()
    };

    public static PropertyDashboardViewModel Map(PropertyDashboardModel model) => new()
    {
        PropertyName = model.Context.PropertyName,
        PropertyTypeLabel = model.Context.PropertyTypeLabel,
        AddressLine = model.Context.AddressLine,
        City = model.Context.City,
        PostalCode = model.Context.PostalCode,
        UnitMetrics = MapMetrics(model.UnitMetrics),
        UnitsByFloor = MapBreakdowns(model.UnitsByFloor),
        TicketMetrics = MapMetrics(model.TicketMetrics),
        TicketsByStatus = MapBreakdowns(model.TicketsByStatus),
        TicketsByPriority = MapBreakdowns(model.TicketsByPriority),
        TicketsByCategory = MapBreakdowns(model.TicketsByCategory),
        RecentTickets = MapTickets(model.RecentTickets),
        ResidentLeaseMetrics = MapMetrics(model.ResidentLeaseMetrics),
        CurrentLeases = model.CurrentLeases.Select(Map).ToList(),
        UpcomingWork = MapWorks(model.UpcomingWork),
        DelayedWork = MapWorks(model.DelayedWork),
        RecentlyCompletedWork = MapWorks(model.RecentlyCompletedWork),
        UnitPreview = model.UnitPreview.Select(Map).ToList()
    };

    public static UnitDashboardViewModel Map(UnitDashboardModel model) => new()
    {
        UnitNr = model.Context.UnitNr,
        FloorNr = model.Context.FloorNr,
        SizeM2 = model.Context.SizeM2,
        PropertyName = model.Context.PropertyName,
        CurrentLease = model.CurrentLease is null ? null : Map(model.CurrentLease),
        TicketMetrics = MapMetrics(model.TicketMetrics),
        TicketsByStatus = MapBreakdowns(model.TicketsByStatus),
        TicketsByPriority = MapBreakdowns(model.TicketsByPriority),
        RecentTickets = MapTickets(model.RecentTickets),
        UpcomingWork = MapWorks(model.UpcomingWork),
        DelayedWork = MapWorks(model.DelayedWork),
        RecentlyCompletedWork = MapWorks(model.RecentlyCompletedWork),
        Timeline = model.Timeline.Select(Map).ToList()
    };

    private static IReadOnlyList<DashboardMetricViewModel> MapMetrics(IReadOnlyList<DashboardMetricModel> metrics) =>
        metrics.Select(metric => new DashboardMetricViewModel
        {
            Key = metric.Key,
            Label = MetricLabel(metric.Key),
            Value = metric.Value
        }).ToList();

    private static IReadOnlyList<DashboardBreakdownViewModel> MapBreakdowns(IReadOnlyList<DashboardBreakdownItemModel> items) =>
        items.Select(item => new DashboardBreakdownViewModel
        {
            Code = item.Code,
            Label = string.IsNullOrWhiteSpace(item.Label) ? T("Unknown", "Unknown") : item.Label,
            Count = item.Count
        }).ToList();

    private static IReadOnlyList<DashboardTicketPreviewViewModel> MapTickets(IReadOnlyList<DashboardTicketPreviewModel> tickets) =>
        tickets.Select(ticket => new DashboardTicketPreviewViewModel
        {
            TicketId = ticket.TicketId,
            TicketNr = ticket.TicketNr,
            Title = ticket.Title,
            StatusCode = ticket.StatusCode,
            StatusLabel = ticket.StatusLabel,
            PriorityLabel = ticket.PriorityLabel,
            DueAt = ticket.DueAt,
            CreatedAt = ticket.CreatedAt,
            CustomerSlug = ticket.CustomerSlug,
            PropertySlug = ticket.PropertySlug,
            UnitSlug = ticket.UnitSlug,
            ResidentIdCode = ticket.ResidentIdCode
        }).ToList();

    private static IReadOnlyList<DashboardWorkPreviewViewModel> MapWorks(IReadOnlyList<DashboardWorkPreviewModel> works) =>
        works.Select(work => new DashboardWorkPreviewViewModel
        {
            ScheduledWorkId = work.ScheduledWorkId,
            TicketId = work.TicketId,
            TicketNr = work.TicketNr,
            TicketTitle = work.TicketTitle,
            VendorName = work.VendorName,
            WorkStatusCode = work.WorkStatusCode,
            WorkStatusLabel = work.WorkStatusLabel,
            ScheduledStart = work.ScheduledStart,
            ScheduledEnd = work.ScheduledEnd,
            RealEnd = work.RealEnd
        }).ToList();

    private static DashboardActivityViewModel Map(DashboardRecentActivityModel activity) => new()
    {
        ItemType = activity.ItemType,
        TypeLabel = ActivityLabel(activity.ItemType),
        Label = activity.Label,
        SupportingText = activity.SupportingText,
        EventAt = activity.EventAt,
        CustomerSlug = activity.CustomerSlug,
        PropertySlug = activity.PropertySlug,
        UnitSlug = activity.UnitSlug,
        ResidentIdCode = activity.ResidentIdCode,
        TicketId = activity.TicketId
    };

    private static DashboardJoinRequestPreviewViewModel Map(DashboardJoinRequestPreviewModel request) => new()
    {
        RequesterName = request.RequesterName,
        RequesterEmail = request.RequesterEmail,
        RequestedRoleLabel = request.RequestedRoleLabel,
        CreatedAt = request.CreatedAt
    };

    private static DashboardRepresentativeViewModel Map(DashboardRepresentativePreviewModel representative) => new()
    {
        ResidentIdCode = representative.ResidentIdCode,
        ResidentName = representative.ResidentName,
        RoleLabel = representative.RoleLabel,
        ValidFrom = representative.ValidFrom,
        ValidTo = representative.ValidTo,
        CustomerSlug = representative.CustomerSlug,
        CustomerName = representative.CustomerName
    };

    private static DashboardLeaseViewModel Map(DashboardLeasePreviewModel lease) => new()
    {
        LeaseId = lease.LeaseId,
        CustomerSlug = lease.CustomerSlug,
        CustomerName = lease.CustomerName,
        PropertySlug = lease.PropertySlug,
        PropertyName = lease.PropertyName,
        UnitSlug = lease.UnitSlug,
        UnitNr = lease.UnitNr,
        ResidentIdCode = lease.ResidentIdCode,
        ResidentName = lease.ResidentName,
        RoleLabel = lease.RoleLabel,
        StartDate = lease.StartDate,
        EndDate = lease.EndDate
    };

    private static DashboardContactSummaryViewModel Map(DashboardContactSummaryModel summary) => new()
    {
        PrimaryContact = summary.PrimaryContact is null
            ? null
            : new DashboardContactPreviewViewModel
            {
                ContactTypeLabel = summary.PrimaryContact.ContactTypeLabel,
                ContactValue = summary.PrimaryContact.ContactValue
            },
        ContactMethodCounts = MapBreakdowns(summary.ContactMethodCounts)
    };

    private static DashboardUnitPreviewViewModel Map(DashboardUnitPreviewModel unit) => new()
    {
        UnitSlug = unit.UnitSlug,
        UnitNr = unit.UnitNr,
        FloorNr = unit.FloorNr,
        SizeM2 = unit.SizeM2,
        HasActiveLease = unit.HasActiveLease,
        CurrentResidentName = unit.CurrentResidentName,
        OpenTicketCount = unit.OpenTicketCount
    };

    private static DashboardTimelineItemViewModel Map(DashboardTimelineItemModel item) => new()
    {
        ItemType = item.ItemType,
        TypeLabel = ActivityLabel(item.ItemType),
        Label = item.Label,
        SupportingText = item.SupportingText,
        EventAt = item.EventAt,
        TicketId = item.TicketId,
        ScheduledWorkId = item.ScheduledWorkId,
        LeaseId = item.LeaseId
    };

    private static string MetricLabel(string key) => key switch
    {
        "customers" => UiText.Customers,
        "properties" => UiText.Properties,
        "units" => UiText.Units,
        "residents" => UiText.Residents,
        "vendors" => T("Vendors", "Vendors"),
        "openTickets" => T("OpenTickets", "Open tickets"),
        "overdueTickets" => T("OverdueTickets", "Overdue tickets"),
        "highPriorityTickets" => T("HighPriorityTickets", "High/urgent tickets"),
        "dueNext7Days" => T("DueNext7Days", "Due next 7 days"),
        "scheduledToday" => T("ScheduledToday", "Scheduled today"),
        "scheduledNext7Days" => T("ScheduledNext7Days", "Scheduled next 7 days"),
        "delayedWork" => T("DelayedWork", "Delayed work"),
        "pendingJoinRequests" => T("PendingJoinRequests", "Pending join requests"),
        "approvedJoinRequests30d" => T("ApprovedLast30Days", "Approved last 30 days"),
        "rejectedJoinRequests30d" => T("RejectedLast30Days", "Rejected last 30 days"),
        "activeUsers" => T("ActiveUsers", "Active users"),
        "expiringAccess" => T("ExpiringAccess", "Expiring access"),
        "activeLeases" => T("ActiveLeases", "Active leases"),
        "connectedResidents" => T("ConnectedResidents", "Connected residents"),
        "totalUnits" => T("TotalUnits", "Total units"),
        "occupiedUnits" => T("OccupiedUnits", "Occupied units"),
        "vacantUnits" => T("VacantUnits", "Vacant units"),
        "knownSquareMeters" => T("KnownSquareMeters", "Known m2"),
        "currentResidents" => T("CurrentResidents", "Current residents"),
        "recentlyClosedTickets" => T("RecentlyClosedTickets", "Recently closed"),
        _ => key
    };

    private static string ActivityLabel(string itemType) => itemType switch
    {
        "customer" => T("Customer", "Customer"),
        "property" => UiText.Property,
        "unit" => UiText.Unit,
        "resident" => UiText.Resident,
        "ticket" => T("Ticket", "Ticket"),
        "lease" => T("Lease", "Lease"),
        "ticketCreated" => T("TicketCreated", "Ticket created"),
        "ticketClosed" => T("TicketClosed", "Ticket closed"),
        "workCompleted" => T("WorkCompleted", "Work completed"),
        _ => itemType
    };

    private static string T(string key, string fallback) =>
        UiText.ResourceManager.GetString(key) ?? fallback;
}
