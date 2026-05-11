using App.BLL.DTO.Dashboards.Models;
using App.DTO.v1.Portal.Dashboards;

namespace App.DTO.v1.Mappers.Portal.Dashboards;

public sealed class PortalDashboardApiMapper
{
    public ManagementDashboardDto Map(ManagementDashboardModel model)
    {
        return new ManagementDashboardDto
        {
            Context = Map(model.Context),
            SummaryMetrics = MapMetrics(model.SummaryMetrics),
            TicketMetrics = MapMetrics(model.TicketMetrics),
            TicketsByStatus = MapBreakdowns(model.TicketsByStatus),
            RecentTickets = MapTickets(model.Context.CompanySlug, model.RecentTickets),
            WorkMetrics = MapMetrics(model.WorkMetrics),
            RecentCompletedWork = MapWorks(model.Context.CompanySlug, model.RecentCompletedWork),
            RecentActivity = model.RecentActivity.Select(activity => Map(model.Context.CompanySlug, activity)).ToList()
        };
    }

    public CustomerDashboardDto Map(CustomerDashboardModel model)
    {
        return new CustomerDashboardDto
        {
            Context = Map(model.Context),
            PortfolioMetrics = MapMetrics(model.PortfolioMetrics),
            TicketMetrics = MapMetrics(model.TicketMetrics),
            TicketsByProperty = MapBreakdowns(model.TicketsByProperty),
            RecentTickets = MapTickets(model.Context.CompanySlug, model.RecentTickets),
            RecentActivity = model.RecentActivity.Select(activity => Map(model.Context.CompanySlug, activity)).ToList()
        };
    }

    public ResidentDashboardDto Map(ResidentDashboardModel model)
    {
        return new ResidentDashboardDto
        {
            Context = Map(model.Context),
            ActiveLeases = model.ActiveLeases.Select(lease => Map(model.Context.CompanySlug, lease)).ToList(),
            TicketMetrics = MapMetrics(model.TicketMetrics),
            RecentTickets = MapTickets(model.Context.CompanySlug, model.RecentTickets),
            ContactSummary = Map(model.ContactSummary),
            Representations = model.Representations.Select(Map).ToList()
        };
    }

    public PropertyDashboardDto Map(PropertyDashboardModel model)
    {
        return new PropertyDashboardDto
        {
            Context = Map(model.Context),
            UnitMetrics = MapMetrics(model.UnitMetrics),
            UnitsByFloor = MapBreakdowns(model.UnitsByFloor),
            TicketMetrics = MapMetrics(model.TicketMetrics),
            TicketsByStatus = MapBreakdowns(model.TicketsByStatus),
            TicketsByPriority = MapBreakdowns(model.TicketsByPriority),
            TicketsByCategory = MapBreakdowns(model.TicketsByCategory),
            RecentTickets = MapTickets(model.Context.CompanySlug, model.RecentTickets),
            ResidentLeaseMetrics = MapMetrics(model.ResidentLeaseMetrics),
            CurrentLeases = model.CurrentLeases.Select(lease => Map(model.Context.CompanySlug, lease)).ToList(),
            UpcomingWork = MapWorks(model.Context.CompanySlug, model.UpcomingWork),
            DelayedWork = MapWorks(model.Context.CompanySlug, model.DelayedWork),
            RecentlyCompletedWork = MapWorks(model.Context.CompanySlug, model.RecentlyCompletedWork),
            UnitPreview = model.UnitPreview.Select(unit => Map(model.Context.CompanySlug, model.Context.CustomerSlug, model.Context.PropertySlug, unit)).ToList()
        };
    }

    public UnitDashboardDto Map(UnitDashboardModel model)
    {
        return new UnitDashboardDto
        {
            Context = Map(model.Context),
            CurrentLease = model.CurrentLease is null ? null : Map(model.Context.CompanySlug, model.CurrentLease),
            TicketMetrics = MapMetrics(model.TicketMetrics),
            TicketsByStatus = MapBreakdowns(model.TicketsByStatus),
            TicketsByPriority = MapBreakdowns(model.TicketsByPriority),
            RecentTickets = MapTickets(model.Context.CompanySlug, model.RecentTickets),
            UpcomingWork = MapWorks(model.Context.CompanySlug, model.UpcomingWork),
            DelayedWork = MapWorks(model.Context.CompanySlug, model.DelayedWork),
            RecentlyCompletedWork = MapWorks(model.Context.CompanySlug, model.RecentlyCompletedWork),
            Timeline = model.Timeline.Select(item => Map(model.Context.CompanySlug, item)).ToList()
        };
    }

    private static ManagementDashboardContextDto Map(ManagementDashboardContextModel model)
    {
        return new ManagementDashboardContextDto
        {
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            RoleCode = model.RoleCode,
            Path = CompanyPath(model.CompanySlug),
            ApiPath = $"{CompanyApiPath(model.CompanySlug)}/dashboard"
        };
    }

    private static CustomerDashboardContextDto Map(CustomerDashboardContextModel model)
    {
        return new CustomerDashboardContextDto
        {
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            RoleCode = model.RoleCode,
            CustomerId = model.CustomerId,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            Phone = model.Phone,
            Path = CustomerPath(model.CompanySlug, model.CustomerSlug),
            ApiPath = $"{CompanyApiPath(model.CompanySlug)}/customers/{Segment(model.CustomerSlug)}/dashboard"
        };
    }

    private static ResidentDashboardContextDto Map(ResidentDashboardContextModel model)
    {
        return new ResidentDashboardContextDto
        {
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            RoleCode = model.RoleCode,
            ResidentId = model.ResidentId,
            ResidentIdCode = model.ResidentIdCode,
            FirstName = model.FirstName,
            LastName = model.LastName,
            FullName = model.FullName,
            PreferredLanguage = model.PreferredLanguage,
            Path = ResidentPath(model.CompanySlug, model.ResidentIdCode),
            ApiPath = $"{CompanyApiPath(model.CompanySlug)}/residents/{Segment(model.ResidentIdCode)}/dashboard"
        };
    }

    private static PropertyDashboardContextDto Map(PropertyDashboardContextModel model)
    {
        return new PropertyDashboardContextDto
        {
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            RoleCode = model.RoleCode,
            CustomerId = model.CustomerId,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            Phone = model.Phone,
            PropertyId = model.PropertyId,
            PropertySlug = model.PropertySlug,
            PropertyName = model.PropertyName,
            PropertyTypeLabel = model.PropertyTypeLabel,
            AddressLine = model.AddressLine,
            City = model.City,
            PostalCode = model.PostalCode,
            Path = PropertyPath(model.CompanySlug, model.CustomerSlug, model.PropertySlug),
            ApiPath = $"{CompanyApiPath(model.CompanySlug)}/customers/{Segment(model.CustomerSlug)}/properties/{Segment(model.PropertySlug)}/dashboard"
        };
    }

    private static UnitDashboardContextDto Map(UnitDashboardContextModel model)
    {
        return new UnitDashboardContextDto
        {
            ManagementCompanyId = model.ManagementCompanyId,
            CompanySlug = model.CompanySlug,
            CompanyName = model.CompanyName,
            RoleCode = model.RoleCode,
            CustomerId = model.CustomerId,
            CustomerSlug = model.CustomerSlug,
            CustomerName = model.CustomerName,
            RegistryCode = model.RegistryCode,
            BillingEmail = model.BillingEmail,
            Phone = model.Phone,
            PropertyId = model.PropertyId,
            PropertySlug = model.PropertySlug,
            PropertyName = model.PropertyName,
            PropertyTypeLabel = model.PropertyTypeLabel,
            AddressLine = model.AddressLine,
            City = model.City,
            PostalCode = model.PostalCode,
            UnitId = model.UnitId,
            UnitSlug = model.UnitSlug,
            UnitNr = model.UnitNr,
            FloorNr = model.FloorNr,
            SizeM2 = model.SizeM2,
            Path = UnitPath(model.CompanySlug, model.CustomerSlug, model.PropertySlug, model.UnitSlug),
            ApiPath = $"{CompanyApiPath(model.CompanySlug)}/customers/{Segment(model.CustomerSlug)}/properties/{Segment(model.PropertySlug)}/units/{Segment(model.UnitSlug)}/dashboard"
        };
    }

    private static IReadOnlyList<DashboardMetricDto> MapMetrics(IReadOnlyList<DashboardMetricModel> metrics)
    {
        return metrics.Select(metric => new DashboardMetricDto
        {
            Key = metric.Key,
            Value = metric.Value
        }).ToList();
    }

    private static IReadOnlyList<DashboardBreakdownItemDto> MapBreakdowns(IReadOnlyList<DashboardBreakdownItemModel> items)
    {
        return items.Select(item => new DashboardBreakdownItemDto
        {
            Code = item.Code,
            Label = item.Label,
            Count = item.Count
        }).ToList();
    }

    private static IReadOnlyList<DashboardTicketPreviewDto> MapTickets(
        string companySlug,
        IReadOnlyList<DashboardTicketPreviewModel> tickets)
    {
        return tickets.Select(ticket => new DashboardTicketPreviewDto
        {
            TicketId = ticket.TicketId,
            TicketNr = ticket.TicketNr,
            Title = ticket.Title,
            StatusCode = ticket.StatusCode,
            StatusLabel = ticket.StatusLabel,
            PriorityCode = ticket.PriorityCode,
            PriorityLabel = ticket.PriorityLabel,
            DueAt = ticket.DueAt,
            CreatedAt = ticket.CreatedAt,
            ClosedAt = ticket.ClosedAt,
            CustomerSlug = ticket.CustomerSlug,
            CustomerName = ticket.CustomerName,
            PropertySlug = ticket.PropertySlug,
            PropertyName = ticket.PropertyName,
            UnitSlug = ticket.UnitSlug,
            UnitNr = ticket.UnitNr,
            ResidentIdCode = ticket.ResidentIdCode,
            ResidentName = ticket.ResidentName,
            Path = TicketPath(companySlug, ticket.TicketId),
            EditPath = $"{TicketPath(companySlug, ticket.TicketId)}/edit"
        }).ToList();
    }

    private static IReadOnlyList<DashboardWorkPreviewDto> MapWorks(
        string companySlug,
        IReadOnlyList<DashboardWorkPreviewModel> works)
    {
        return works.Select(work => new DashboardWorkPreviewDto
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
            RealEnd = work.RealEnd,
            Path = ScheduledWorkPath(companySlug, work.TicketId, work.ScheduledWorkId),
            EditPath = $"{ScheduledWorkPath(companySlug, work.TicketId, work.ScheduledWorkId)}/edit",
            TicketPath = TicketPath(companySlug, work.TicketId)
        }).ToList();
    }

    private static DashboardRecentActivityDto Map(string companySlug, DashboardRecentActivityModel activity)
    {
        return new DashboardRecentActivityDto
        {
            ItemType = activity.ItemType,
            Label = activity.Label,
            SupportingText = activity.SupportingText,
            EventAt = activity.EventAt,
            CustomerSlug = activity.CustomerSlug,
            PropertySlug = activity.PropertySlug,
            UnitSlug = activity.UnitSlug,
            ResidentIdCode = activity.ResidentIdCode,
            TicketId = activity.TicketId,
            Path = ActivityPath(companySlug, activity)
        };
    }

    private static DashboardRepresentativePreviewDto Map(DashboardRepresentativePreviewModel representative)
    {
        return new DashboardRepresentativePreviewDto
        {
            RepresentativeId = representative.RepresentativeId,
            ResidentIdCode = representative.ResidentIdCode,
            ResidentName = representative.ResidentName,
            RoleCode = representative.RoleCode,
            RoleLabel = representative.RoleLabel,
            ValidFrom = representative.ValidFrom,
            ValidTo = representative.ValidTo,
            CustomerSlug = representative.CustomerSlug,
            CustomerName = representative.CustomerName
        };
    }

    private static DashboardLeasePreviewDto Map(string companySlug, DashboardLeasePreviewModel lease)
    {
        return new DashboardLeasePreviewDto
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
            RoleCode = lease.RoleCode,
            RoleLabel = lease.RoleLabel,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            CreatedAt = lease.CreatedAt,
            Path = UnitPath(companySlug, lease.CustomerSlug, lease.PropertySlug, lease.UnitSlug)
        };
    }

    private static DashboardContactSummaryDto Map(DashboardContactSummaryModel summary)
    {
        return new DashboardContactSummaryDto
        {
            PrimaryContact = summary.PrimaryContact is null
                ? null
                : new DashboardContactPreviewDto
                {
                    ContactId = summary.PrimaryContact.ContactId,
                    ContactTypeCode = summary.PrimaryContact.ContactTypeCode,
                    ContactTypeLabel = summary.PrimaryContact.ContactTypeLabel,
                    ContactValue = summary.PrimaryContact.ContactValue
                },
            ContactMethodCounts = MapBreakdowns(summary.ContactMethodCounts)
        };
    }

    private static DashboardUnitPreviewDto Map(
        string companySlug,
        string customerSlug,
        string propertySlug,
        DashboardUnitPreviewModel unit)
    {
        var path = UnitPath(companySlug, customerSlug, propertySlug, unit.UnitSlug);

        return new DashboardUnitPreviewDto
        {
            UnitId = unit.UnitId,
            UnitSlug = unit.UnitSlug,
            UnitNr = unit.UnitNr,
            FloorNr = unit.FloorNr,
            SizeM2 = unit.SizeM2,
            HasActiveLease = unit.HasActiveLease,
            CurrentResidentName = unit.CurrentResidentName,
            OpenTicketCount = unit.OpenTicketCount,
            Path = path,
            EditPath = $"{path}/profile"
        };
    }

    private static DashboardTimelineItemDto Map(string companySlug, DashboardTimelineItemModel item)
    {
        return new DashboardTimelineItemDto
        {
            ItemType = item.ItemType,
            Label = item.Label,
            SupportingText = item.SupportingText,
            EventAt = item.EventAt,
            TicketId = item.TicketId,
            ScheduledWorkId = item.ScheduledWorkId,
            LeaseId = item.LeaseId,
            Path = TimelinePath(companySlug, item)
        };
    }

    private static string ActivityPath(string companySlug, DashboardRecentActivityModel activity)
    {
        if (activity.TicketId is not null)
        {
            return TicketPath(companySlug, activity.TicketId.Value);
        }

        if (!string.IsNullOrWhiteSpace(activity.CustomerSlug)
            && !string.IsNullOrWhiteSpace(activity.PropertySlug)
            && !string.IsNullOrWhiteSpace(activity.UnitSlug))
        {
            return UnitPath(companySlug, activity.CustomerSlug, activity.PropertySlug, activity.UnitSlug);
        }

        if (!string.IsNullOrWhiteSpace(activity.CustomerSlug)
            && !string.IsNullOrWhiteSpace(activity.PropertySlug))
        {
            return PropertyPath(companySlug, activity.CustomerSlug, activity.PropertySlug);
        }

        if (!string.IsNullOrWhiteSpace(activity.CustomerSlug))
        {
            return CustomerPath(companySlug, activity.CustomerSlug);
        }

        return !string.IsNullOrWhiteSpace(activity.ResidentIdCode)
            ? ResidentPath(companySlug, activity.ResidentIdCode)
            : CompanyPath(companySlug);
    }

    private static string TimelinePath(string companySlug, DashboardTimelineItemModel item)
    {
        if (item.ScheduledWorkId is not null && item.TicketId is not null)
        {
            return ScheduledWorkPath(companySlug, item.TicketId.Value, item.ScheduledWorkId.Value);
        }

        return item.TicketId is null
            ? CompanyPath(companySlug)
            : TicketPath(companySlug, item.TicketId.Value);
    }

    private static string CompanyApiPath(string companySlug) => $"/api/v1/portal/companies/{Segment(companySlug)}";

    private static string CompanyPath(string companySlug) => $"/companies/{Segment(companySlug)}";

    private static string CustomerPath(string companySlug, string customerSlug) =>
        $"{CompanyPath(companySlug)}/customers/{Segment(customerSlug)}";

    private static string PropertyPath(string companySlug, string customerSlug, string propertySlug) =>
        $"{CustomerPath(companySlug, customerSlug)}/properties/{Segment(propertySlug)}";

    private static string UnitPath(string companySlug, string customerSlug, string propertySlug, string unitSlug) =>
        $"{PropertyPath(companySlug, customerSlug, propertySlug)}/units/{Segment(unitSlug)}";

    private static string ResidentPath(string companySlug, string residentIdCode) =>
        $"{CompanyPath(companySlug)}/residents/{Segment(residentIdCode)}";

    private static string TicketPath(string companySlug, Guid ticketId) =>
        $"{CompanyPath(companySlug)}/tickets/{ticketId:D}";

    private static string ScheduledWorkPath(string companySlug, Guid ticketId, Guid scheduledWorkId) =>
        $"{TicketPath(companySlug, ticketId)}/scheduled-work/{scheduledWorkId:D}";

    private static string Segment(string value) => Uri.EscapeDataString(value);
}
