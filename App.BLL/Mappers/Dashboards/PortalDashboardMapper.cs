using App.BLL.DTO.Dashboards.Models;
using App.DAL.DTO.Dashboards;

namespace App.BLL.Mappers.Dashboards;

public static class PortalDashboardMapper
{
    public static ManagementDashboardModel Map(ManagementDashboardDalDto dto) => new()
    {
        Context = Map(dto.Context),
        SummaryMetrics = MapMetrics(dto.SummaryMetrics),
        TicketMetrics = MapMetrics(dto.TicketMetrics),
        TicketsByStatus = MapBreakdowns(dto.TicketsByStatus),
        RecentTickets = MapTickets(dto.RecentTickets),
        WorkMetrics = MapMetrics(dto.WorkMetrics),
        RecentCompletedWork = MapWorks(dto.RecentCompletedWork),
        RecentActivity = dto.RecentActivity.Select(Map).ToList()
    };

    public static CustomerDashboardModel Map(CustomerDashboardDalDto dto) => new()
    {
        Context = Map(dto.Context),
        PortfolioMetrics = MapMetrics(dto.PortfolioMetrics),
        TicketMetrics = MapMetrics(dto.TicketMetrics),
        TicketsByProperty = MapBreakdowns(dto.TicketsByProperty),
        RecentTickets = MapTickets(dto.RecentTickets),
        RecentActivity = dto.RecentActivity.Select(Map).ToList()
    };

    public static ResidentDashboardModel Map(ResidentDashboardDalDto dto) => new()
    {
        Context = Map(dto.Context),
        ActiveLeases = dto.ActiveLeases.Select(Map).ToList(),
        TicketMetrics = MapMetrics(dto.TicketMetrics),
        RecentTickets = MapTickets(dto.RecentTickets),
        ContactSummary = Map(dto.ContactSummary),
        Representations = dto.Representations.Select(Map).ToList()
    };

    public static PropertyDashboardModel Map(PropertyDashboardDalDto dto) => new()
    {
        Context = Map(dto.Context),
        UnitMetrics = MapMetrics(dto.UnitMetrics),
        UnitsByFloor = MapBreakdowns(dto.UnitsByFloor),
        TicketMetrics = MapMetrics(dto.TicketMetrics),
        TicketsByStatus = MapBreakdowns(dto.TicketsByStatus),
        TicketsByPriority = MapBreakdowns(dto.TicketsByPriority),
        TicketsByCategory = MapBreakdowns(dto.TicketsByCategory),
        RecentTickets = MapTickets(dto.RecentTickets),
        ResidentLeaseMetrics = MapMetrics(dto.ResidentLeaseMetrics),
        CurrentLeases = dto.CurrentLeases.Select(Map).ToList(),
        UpcomingWork = MapWorks(dto.UpcomingWork),
        DelayedWork = MapWorks(dto.DelayedWork),
        RecentlyCompletedWork = MapWorks(dto.RecentlyCompletedWork),
        UnitPreview = dto.UnitPreview.Select(Map).ToList()
    };

    public static UnitDashboardModel Map(UnitDashboardDalDto dto) => new()
    {
        Context = Map(dto.Context),
        CurrentLease = dto.CurrentLease is null ? null : Map(dto.CurrentLease),
        TicketMetrics = MapMetrics(dto.TicketMetrics),
        TicketsByStatus = MapBreakdowns(dto.TicketsByStatus),
        TicketsByPriority = MapBreakdowns(dto.TicketsByPriority),
        RecentTickets = MapTickets(dto.RecentTickets),
        UpcomingWork = MapWorks(dto.UpcomingWork),
        DelayedWork = MapWorks(dto.DelayedWork),
        RecentlyCompletedWork = MapWorks(dto.RecentlyCompletedWork),
        Timeline = dto.Timeline.Select(Map).ToList()
    };

    private static ManagementDashboardContextModel Map(ManagementDashboardContextDalDto dto) => new()
    {
        ManagementCompanyId = dto.ManagementCompanyId,
        CompanySlug = dto.CompanySlug,
        CompanyName = dto.CompanyName,
        RoleCode = dto.RoleCode
    };

    private static CustomerDashboardContextModel Map(CustomerDashboardContextDalDto dto) => new()
    {
        ManagementCompanyId = dto.ManagementCompanyId,
        CompanySlug = dto.CompanySlug,
        CompanyName = dto.CompanyName,
        RoleCode = dto.RoleCode,
        CustomerId = dto.CustomerId,
        CustomerSlug = dto.CustomerSlug,
        CustomerName = dto.CustomerName,
        RegistryCode = dto.RegistryCode,
        BillingEmail = dto.BillingEmail,
        Phone = dto.Phone
    };

    private static ResidentDashboardContextModel Map(ResidentDashboardContextDalDto dto) => new()
    {
        ManagementCompanyId = dto.ManagementCompanyId,
        CompanySlug = dto.CompanySlug,
        CompanyName = dto.CompanyName,
        RoleCode = dto.RoleCode,
        ResidentId = dto.ResidentId,
        ResidentIdCode = dto.ResidentIdCode,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        FullName = dto.FullName,
        PreferredLanguage = dto.PreferredLanguage
    };

    private static PropertyDashboardContextModel Map(PropertyDashboardContextDalDto dto) => new()
    {
        ManagementCompanyId = dto.ManagementCompanyId,
        CompanySlug = dto.CompanySlug,
        CompanyName = dto.CompanyName,
        RoleCode = dto.RoleCode,
        CustomerId = dto.CustomerId,
        CustomerSlug = dto.CustomerSlug,
        CustomerName = dto.CustomerName,
        RegistryCode = dto.RegistryCode,
        BillingEmail = dto.BillingEmail,
        Phone = dto.Phone,
        PropertyId = dto.PropertyId,
        PropertySlug = dto.PropertySlug,
        PropertyName = dto.PropertyName,
        PropertyTypeLabel = dto.PropertyTypeLabel,
        AddressLine = dto.AddressLine,
        City = dto.City,
        PostalCode = dto.PostalCode
    };

    private static UnitDashboardContextModel Map(UnitDashboardContextDalDto dto) => new()
    {
        ManagementCompanyId = dto.ManagementCompanyId,
        CompanySlug = dto.CompanySlug,
        CompanyName = dto.CompanyName,
        RoleCode = dto.RoleCode,
        CustomerId = dto.CustomerId,
        CustomerSlug = dto.CustomerSlug,
        CustomerName = dto.CustomerName,
        RegistryCode = dto.RegistryCode,
        BillingEmail = dto.BillingEmail,
        Phone = dto.Phone,
        PropertyId = dto.PropertyId,
        PropertySlug = dto.PropertySlug,
        PropertyName = dto.PropertyName,
        PropertyTypeLabel = dto.PropertyTypeLabel,
        AddressLine = dto.AddressLine,
        City = dto.City,
        PostalCode = dto.PostalCode,
        UnitId = dto.UnitId,
        UnitSlug = dto.UnitSlug,
        UnitNr = dto.UnitNr,
        FloorNr = dto.FloorNr,
        SizeM2 = dto.SizeM2
    };

    private static IReadOnlyList<DashboardMetricModel> MapMetrics(IReadOnlyList<DashboardMetricDalDto> metrics) =>
        metrics.Select(metric => new DashboardMetricModel { Key = metric.Key, Value = metric.Value }).ToList();

    private static IReadOnlyList<DashboardBreakdownItemModel> MapBreakdowns(IReadOnlyList<DashboardBreakdownItemDalDto> items) =>
        items.Select(item => new DashboardBreakdownItemModel { Code = item.Code, Label = item.Label, Count = item.Count }).ToList();

    private static IReadOnlyList<DashboardTicketPreviewModel> MapTickets(IReadOnlyList<DashboardTicketPreviewDalDto> tickets) =>
        tickets.Select(Map).ToList();

    private static DashboardTicketPreviewModel Map(DashboardTicketPreviewDalDto dto) => new()
    {
        TicketId = dto.TicketId,
        TicketNr = dto.TicketNr,
        Title = dto.Title,
        StatusCode = dto.StatusCode,
        StatusLabel = dto.StatusLabel,
        PriorityCode = dto.PriorityCode,
        PriorityLabel = dto.PriorityLabel,
        DueAt = dto.DueAt,
        CreatedAt = dto.CreatedAt,
        ClosedAt = dto.ClosedAt,
        CustomerSlug = dto.CustomerSlug,
        CustomerName = dto.CustomerName,
        PropertySlug = dto.PropertySlug,
        PropertyName = dto.PropertyName,
        UnitSlug = dto.UnitSlug,
        UnitNr = dto.UnitNr,
        ResidentIdCode = dto.ResidentIdCode,
        ResidentName = dto.ResidentName
    };

    private static IReadOnlyList<DashboardWorkPreviewModel> MapWorks(IReadOnlyList<DashboardWorkPreviewDalDto> works) =>
        works.Select(work => new DashboardWorkPreviewModel
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
            RealStart = work.RealStart,
            RealEnd = work.RealEnd
        }).ToList();

    private static DashboardRecentActivityModel Map(DashboardRecentActivityDalDto dto) => new()
    {
        ItemType = dto.ItemType,
        Label = dto.Label,
        SupportingText = dto.SupportingText,
        EventAt = dto.EventAt,
        CustomerSlug = dto.CustomerSlug,
        PropertySlug = dto.PropertySlug,
        UnitSlug = dto.UnitSlug,
        ResidentIdCode = dto.ResidentIdCode,
        TicketId = dto.TicketId
    };

    private static DashboardRepresentativePreviewModel Map(DashboardRepresentativePreviewDalDto dto) => new()
    {
        RepresentativeId = dto.RepresentativeId,
        ResidentIdCode = dto.ResidentIdCode,
        ResidentName = dto.ResidentName,
        RoleCode = dto.RoleCode,
        RoleLabel = dto.RoleLabel,
        ValidFrom = dto.ValidFrom,
        ValidTo = dto.ValidTo,
        CustomerSlug = dto.CustomerSlug,
        CustomerName = dto.CustomerName
    };

    private static DashboardLeasePreviewModel Map(DashboardLeasePreviewDalDto dto) => new()
    {
        LeaseId = dto.LeaseId,
        CustomerSlug = dto.CustomerSlug,
        CustomerName = dto.CustomerName,
        PropertySlug = dto.PropertySlug,
        PropertyName = dto.PropertyName,
        UnitSlug = dto.UnitSlug,
        UnitNr = dto.UnitNr,
        ResidentIdCode = dto.ResidentIdCode,
        ResidentName = dto.ResidentName,
        RoleCode = dto.RoleCode,
        RoleLabel = dto.RoleLabel,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        CreatedAt = dto.CreatedAt
    };

    private static DashboardContactSummaryModel Map(DashboardContactSummaryDalDto dto) => new()
    {
        PrimaryContact = dto.PrimaryContact is null
            ? null
            : new DashboardContactPreviewModel
            {
                ContactId = dto.PrimaryContact.ContactId,
                ContactTypeCode = dto.PrimaryContact.ContactTypeCode,
                ContactTypeLabel = dto.PrimaryContact.ContactTypeLabel,
                ContactValue = dto.PrimaryContact.ContactValue
            },
        ContactMethodCounts = MapBreakdowns(dto.ContactMethodCounts)
    };

    private static DashboardUnitPreviewModel Map(DashboardUnitPreviewDalDto dto) => new()
    {
        UnitId = dto.UnitId,
        UnitSlug = dto.UnitSlug,
        UnitNr = dto.UnitNr,
        FloorNr = dto.FloorNr,
        SizeM2 = dto.SizeM2,
        HasActiveLease = dto.HasActiveLease,
        CurrentResidentName = dto.CurrentResidentName,
        OpenTicketCount = dto.OpenTicketCount
    };

    private static DashboardTimelineItemModel Map(DashboardTimelineItemDalDto dto) => new()
    {
        ItemType = dto.ItemType,
        Label = dto.Label,
        SupportingText = dto.SupportingText,
        EventAt = dto.EventAt,
        TicketId = dto.TicketId,
        ScheduledWorkId = dto.ScheduledWorkId,
        LeaseId = dto.LeaseId
    };
}
