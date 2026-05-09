using App.DAL.Contracts.Repositories.Dashboards;
using App.DAL.DTO.Dashboards;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories.Dashboards;

public class PortalDashboardRepository : IPortalDashboardRepository
{
    private readonly AppDbContext _dbContext;

    public PortalDashboardRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ManagementDashboardDalDto> GetManagementDashboardAsync(
        Guid managementCompanyId,
        string companySlug,
        string companyName,
        string roleCode,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default)
    {
        var tickets = CompanyTickets(managementCompanyId);
        var openTickets = OpenTickets(tickets, options);
        var work = CompanyWork(managementCompanyId);

        var summaryMetrics = new List<DashboardMetricDalDto>
        {
            Metric("customers", await _dbContext.Customers.CountAsync(customer => customer.ManagementCompanyId == managementCompanyId, cancellationToken)),
            Metric("properties", await _dbContext.Properties.CountAsync(property => property.Customer!.ManagementCompanyId == managementCompanyId, cancellationToken)),
            Metric("units", await _dbContext.Units.CountAsync(unit => unit.Property!.Customer!.ManagementCompanyId == managementCompanyId, cancellationToken)),
            Metric("residents", await _dbContext.Residents.CountAsync(resident => resident.ManagementCompanyId == managementCompanyId, cancellationToken)),
            Metric("vendors", await _dbContext.Vendors.CountAsync(vendor => vendor.ManagementCompanyId == managementCompanyId, cancellationToken)),
            Metric("openTickets", await openTickets.CountAsync(cancellationToken))
        };

        return new ManagementDashboardDalDto
        {
            Context = new ManagementDashboardContextDalDto
            {
                ManagementCompanyId = managementCompanyId,
                CompanySlug = companySlug,
                CompanyName = companyName,
                RoleCode = roleCode
            },
            SummaryMetrics = summaryMetrics,
            TicketMetrics = await BuildTicketMetricsAsync(tickets, options, cancellationToken),
            TicketsByStatus = await BuildTicketStatusBreakdownAsync(tickets, cancellationToken),
            RecentTickets = await ProjectTickets(tickets)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            WorkMetrics = await BuildWorkMetricsAsync(work, options, cancellationToken),
            RecentCompletedWork = await ProjectWork(work.Where(item =>
                    item.RealEnd != null
                    && item.RealEnd >= options.RecentSinceUtc
                    && item.WorkStatus!.Code == "DONE"))
                .OrderByDescending(item => item.RealEnd)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            RecentActivity = await BuildManagementRecentActivityAsync(managementCompanyId, options, cancellationToken)
        };
    }

    public async Task<CustomerDashboardDalDto> GetCustomerDashboardAsync(
        Guid managementCompanyId,
        Guid customerId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default)
    {
        var context = await CustomerContext(managementCompanyId, customerId)
            .FirstAsync(cancellationToken);
        var tickets = CompanyTickets(managementCompanyId)
            .Where(ticket => ticket.CustomerId == customerId);
        var activeLeases = ActiveLeases(options)
            .Where(lease => lease.Unit!.Property!.CustomerId == customerId
                            && lease.Unit.Property.Customer!.ManagementCompanyId == managementCompanyId);

        return new CustomerDashboardDalDto
        {
            Context = context,
            PortfolioMetrics = new List<DashboardMetricDalDto>
            {
                Metric("properties", await _dbContext.Properties.CountAsync(property => property.CustomerId == customerId && property.Customer!.ManagementCompanyId == managementCompanyId, cancellationToken)),
                Metric("units", await _dbContext.Units.CountAsync(unit => unit.Property!.CustomerId == customerId && unit.Property.Customer!.ManagementCompanyId == managementCompanyId, cancellationToken)),
                Metric("activeLeases", await activeLeases.CountAsync(cancellationToken)),
                Metric("connectedResidents", await activeLeases.Select(lease => lease.ResidentId).Distinct().CountAsync(cancellationToken))
            },
            TicketMetrics = await BuildTicketMetricsAsync(tickets, options, cancellationToken),
            TicketsByProperty = await BuildPropertyTicketBreakdownAsync(tickets, cancellationToken),
            RecentTickets = await ProjectTickets(tickets)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            RecentActivity = await BuildCustomerRecentActivityAsync(managementCompanyId, customerId, options, cancellationToken)
        };
    }

    public async Task<PropertyDashboardDalDto> GetPropertyDashboardAsync(
        Guid managementCompanyId,
        Guid customerId,
        Guid propertyId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default)
    {
        var context = await PropertyContext(managementCompanyId, customerId, propertyId)
            .FirstAsync(cancellationToken);
        var units = _dbContext.Units.AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId
                           && unit.Property!.CustomerId == customerId
                           && unit.Property.Customer!.ManagementCompanyId == managementCompanyId);
        var tickets = CompanyTickets(managementCompanyId)
            .Where(ticket => ticket.CustomerId == customerId && ticket.PropertyId == propertyId);
        var activeLeases = ActiveLeases(options)
            .Where(lease => lease.Unit!.PropertyId == propertyId
                            && lease.Unit.Property!.CustomerId == customerId
                            && lease.Unit.Property.Customer!.ManagementCompanyId == managementCompanyId);
        var work = CompanyWork(managementCompanyId)
            .Where(item => item.Ticket!.PropertyId == propertyId);
        var occupiedUnitIds = activeLeases.Select(lease => lease.UnitId).Distinct();

        return new PropertyDashboardDalDto
        {
            Context = context,
            UnitMetrics = new List<DashboardMetricDalDto>
            {
                Metric("totalUnits", await units.CountAsync(cancellationToken)),
                Metric("occupiedUnits", await occupiedUnitIds.CountAsync(cancellationToken)),
                Metric("vacantUnits", await units.CountAsync(cancellationToken) - await occupiedUnitIds.CountAsync(cancellationToken)),
                Metric("knownSquareMeters", Convert.ToInt32(await units.SumAsync(unit => unit.SizeM2 ?? 0m, cancellationToken)))
            },
            UnitsByFloor = await BuildUnitsByFloorBreakdownAsync(units, cancellationToken),
            TicketMetrics = await BuildTicketMetricsAsync(tickets, options, cancellationToken),
            TicketsByStatus = await BuildTicketStatusBreakdownAsync(tickets, cancellationToken),
            TicketsByPriority = await BuildTicketPriorityBreakdownAsync(tickets, cancellationToken),
            TicketsByCategory = await BuildTicketCategoryBreakdownAsync(tickets, cancellationToken),
            RecentTickets = await ProjectTickets(tickets)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            ResidentLeaseMetrics = new List<DashboardMetricDalDto>
            {
                Metric("currentResidents", await activeLeases.Select(lease => lease.ResidentId).Distinct().CountAsync(cancellationToken)),
                Metric("activeLeases", await activeLeases.CountAsync(cancellationToken))
            },
            CurrentLeases = await ProjectLeases(activeLeases)
                .OrderBy(lease => lease.UnitNr)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            UpcomingWork = await ProjectWork(work.Where(item =>
                    item.ScheduledStart >= options.TodayStartUtc
                    && item.ScheduledStart < options.NextSevenDaysEndUtc))
                .OrderBy(item => item.ScheduledStart)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            DelayedWork = await ProjectWork(DelayedWork(work, options))
                .OrderBy(item => item.ScheduledStart)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            RecentlyCompletedWork = await ProjectWork(work.Where(item =>
                    item.RealEnd != null
                    && item.RealEnd >= options.RecentSinceUtc
                    && item.WorkStatus!.Code == "DONE"))
                .OrderByDescending(item => item.RealEnd)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            UnitPreview = await ProjectUnitPreview(units, options)
                .OrderBy(unit => unit.UnitNr)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<UnitDashboardDalDto> GetUnitDashboardAsync(
        Guid managementCompanyId,
        Guid propertyId,
        Guid unitId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default)
    {
        var context = await UnitContext(managementCompanyId, propertyId, unitId)
            .FirstAsync(cancellationToken);
        var tickets = CompanyTickets(managementCompanyId)
            .Where(ticket => ticket.PropertyId == propertyId && ticket.UnitId == unitId);
        var work = CompanyWork(managementCompanyId)
            .Where(item => item.Ticket!.UnitId == unitId);
        var leases = _dbContext.Leases.AsNoTracking()
            .Where(lease => lease.UnitId == unitId
                            && lease.Unit!.PropertyId == propertyId
                            && lease.Unit.Property!.Customer!.ManagementCompanyId == managementCompanyId);

        var currentLease = await ProjectLeases(ActiveLeases(options).Where(lease =>
                lease.UnitId == unitId
                && lease.Unit!.PropertyId == propertyId
                && lease.Unit.Property!.Customer!.ManagementCompanyId == managementCompanyId))
            .OrderByDescending(lease => lease.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return new UnitDashboardDalDto
        {
            Context = context,
            CurrentLease = currentLease,
            TicketMetrics = new List<DashboardMetricDalDto>
            {
                Metric("openTickets", await OpenTickets(tickets, options).CountAsync(cancellationToken)),
                Metric("overdueTickets", await OverdueTickets(tickets, options).CountAsync(cancellationToken)),
                Metric("recentlyClosedTickets", await tickets.CountAsync(ticket => ticket.ClosedAt != null && ticket.ClosedAt >= options.RecentSinceUtc, cancellationToken))
            },
            TicketsByStatus = await BuildTicketStatusBreakdownAsync(tickets, cancellationToken),
            TicketsByPriority = await BuildTicketPriorityBreakdownAsync(tickets, cancellationToken),
            RecentTickets = await ProjectTickets(tickets)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            UpcomingWork = await ProjectWork(work.Where(item =>
                    item.ScheduledStart >= options.TodayStartUtc
                    && item.ScheduledStart < options.NextSevenDaysEndUtc))
                .OrderBy(item => item.ScheduledStart)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            DelayedWork = await ProjectWork(DelayedWork(work, options))
                .OrderBy(item => item.ScheduledStart)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            RecentlyCompletedWork = await ProjectWork(work.Where(item =>
                    item.RealEnd != null
                    && item.RealEnd >= options.RecentSinceUtc
                    && item.WorkStatus!.Code == "DONE"))
                .OrderByDescending(item => item.RealEnd)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            Timeline = await BuildUnitTimelineAsync(tickets, leases, work, options, cancellationToken)
        };
    }

    public async Task<ResidentDashboardDalDto> GetResidentDashboardAsync(
        Guid managementCompanyId,
        Guid residentId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken = default)
    {
        var context = await ResidentContext(managementCompanyId, residentId)
            .FirstAsync(cancellationToken);
        var tickets = CompanyTickets(managementCompanyId)
            .Where(ticket => ticket.ResidentId == residentId);
        var activeLeases = ActiveLeases(options)
            .Where(lease => lease.ResidentId == residentId
                            && lease.Resident!.ManagementCompanyId == managementCompanyId);
        var representations = ActiveRepresentatives(options)
            .Where(representative => representative.ResidentId == residentId
                                     && representative.Resident!.ManagementCompanyId == managementCompanyId);
        var contacts = _dbContext.ResidentContacts.AsNoTracking()
            .Where(contact => contact.ResidentId == residentId
                              && contact.Resident!.ManagementCompanyId == managementCompanyId
                              && contact.ValidFrom <= options.TodayDate
                              && (contact.ValidTo == null || contact.ValidTo >= options.TodayDate));

        return new ResidentDashboardDalDto
        {
            Context = context,
            ActiveLeases = await ProjectLeases(activeLeases)
                .OrderByDescending(lease => lease.StartDate)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            TicketMetrics = new List<DashboardMetricDalDto>
            {
                Metric("openTickets", await OpenTickets(tickets, options).CountAsync(cancellationToken)),
                Metric("overdueTickets", await OverdueTickets(tickets, options).CountAsync(cancellationToken)),
                Metric("recentlyClosedTickets", await tickets.CountAsync(ticket => ticket.ClosedAt != null && ticket.ClosedAt >= options.RecentSinceUtc, cancellationToken))
            },
            RecentTickets = await ProjectTickets(tickets)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken),
            ContactSummary = new DashboardContactSummaryDalDto
            {
                PrimaryContact = await contacts
                    .Where(contact => contact.IsPrimary)
                    .Select(contact => new DashboardContactPreviewDalDto
                    {
                        ContactId = contact.ContactId,
                        ContactTypeCode = contact.Contact!.ContactType!.Code,
                        ContactTypeLabel = contact.Contact.ContactType.Label.ToString(),
                        ContactValue = contact.Contact.ContactValue
                    })
                    .FirstOrDefaultAsync(cancellationToken),
                ContactMethodCounts = await BuildContactTypeBreakdownAsync(contacts, cancellationToken)
            },
            Representations = await ProjectRepresentatives(representations)
                .OrderBy(representative => representative.CustomerName)
                .Take(options.PreviewLimit)
                .ToListAsync(cancellationToken)
        };
    }

    private IQueryable<App.Domain.Ticket> CompanyTickets(Guid managementCompanyId) =>
        _dbContext.Tickets.AsNoTracking()
            .Where(ticket => ticket.ManagementCompanyId == managementCompanyId);

    private IQueryable<App.Domain.ScheduledWork> CompanyWork(Guid managementCompanyId) =>
        _dbContext.ScheduledWorks.AsNoTracking()
            .Where(work => work.Ticket!.ManagementCompanyId == managementCompanyId);

    private IQueryable<App.Domain.Lease> ActiveLeases(PortalDashboardQueryOptionsDalDto options) =>
        _dbContext.Leases.AsNoTracking()
            .Where(lease => lease.StartDate <= options.TodayDate
                            && (lease.EndDate == null || lease.EndDate >= options.TodayDate));

    private IQueryable<App.Domain.CustomerRepresentative> ActiveRepresentatives(PortalDashboardQueryOptionsDalDto options) =>
        _dbContext.CustomerRepresentatives.AsNoTracking()
            .Where(representative => representative.ValidFrom <= options.TodayDate
                                     && (representative.ValidTo == null || representative.ValidTo >= options.TodayDate));

    private static IQueryable<App.Domain.Ticket> OpenTickets(
        IQueryable<App.Domain.Ticket> tickets,
        PortalDashboardQueryOptionsDalDto options)
    {
        var closedCodes = options.OpenTicketExcludedStatusCodes.ToList();
        return tickets.Where(ticket => !closedCodes.Contains(ticket.TicketStatus!.Code));
    }

    private static IQueryable<App.Domain.Ticket> OverdueTickets(
        IQueryable<App.Domain.Ticket> tickets,
        PortalDashboardQueryOptionsDalDto options) =>
        OpenTickets(tickets, options)
            .Where(ticket => ticket.DueAt != null && ticket.DueAt < options.UtcNow);

    private static IQueryable<App.Domain.Ticket> HighPriorityTickets(
        IQueryable<App.Domain.Ticket> tickets,
        PortalDashboardQueryOptionsDalDto options)
    {
        var highPriorityCodes = options.HighPriorityCodes.ToList();
        return tickets.Where(ticket => highPriorityCodes.Contains(ticket.TicketPriority!.Code));
    }

    private static IQueryable<App.Domain.ScheduledWork> DelayedWork(
        IQueryable<App.Domain.ScheduledWork> work,
        PortalDashboardQueryOptionsDalDto options)
    {
        var completedCodes = options.CompletedOrCancelledWorkStatusCodes.ToList();
        return work.Where(item =>
            (item.ScheduledStart < options.UtcNow || (item.ScheduledEnd != null && item.ScheduledEnd < options.UtcNow))
            && !completedCodes.Contains(item.WorkStatus!.Code));
    }

    private async Task<IReadOnlyList<DashboardBreakdownItemDalDto>> BuildTicketStatusBreakdownAsync(
        IQueryable<App.Domain.Ticket> tickets,
        CancellationToken cancellationToken)
    {
        var counts = await tickets
            .GroupBy(ticket => ticket.TicketStatus!.Code)
            .Select(group => new { Code = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var codes = counts.Select(item => item.Code).ToList();
        var labels = await _dbContext.TicketStatuses
            .AsNoTracking()
            .Where(status => codes.Contains(status.Code))
            .Select(status => new { status.Code, Label = status.Label.ToString() })
            .ToDictionaryAsync(item => item.Code, item => item.Label, cancellationToken);

        return counts
            .Select(item => Breakdown(item.Code, labels.GetValueOrDefault(item.Code), item.Count))
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardBreakdownItemDalDto>> BuildTicketPriorityBreakdownAsync(
        IQueryable<App.Domain.Ticket> tickets,
        CancellationToken cancellationToken)
    {
        var counts = await tickets
            .GroupBy(ticket => ticket.TicketPriority!.Code)
            .Select(group => new { Code = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var codes = counts.Select(item => item.Code).ToList();
        var labels = await _dbContext.TicketPriorities
            .AsNoTracking()
            .Where(priority => codes.Contains(priority.Code))
            .Select(priority => new { priority.Code, Label = priority.Label.ToString() })
            .ToDictionaryAsync(item => item.Code, item => item.Label, cancellationToken);

        return counts
            .Select(item => Breakdown(item.Code, labels.GetValueOrDefault(item.Code), item.Count))
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardBreakdownItemDalDto>> BuildTicketCategoryBreakdownAsync(
        IQueryable<App.Domain.Ticket> tickets,
        CancellationToken cancellationToken)
    {
        var counts = await tickets
            .GroupBy(ticket => ticket.TicketCategory!.Code)
            .Select(group => new { Code = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var codes = counts.Select(item => item.Code).ToList();
        var labels = await _dbContext.TicketCategories
            .AsNoTracking()
            .Where(category => codes.Contains(category.Code))
            .Select(category => new { category.Code, Label = category.Label.ToString() })
            .ToDictionaryAsync(item => item.Code, item => item.Label, cancellationToken);

        return counts
            .Select(item => Breakdown(item.Code, labels.GetValueOrDefault(item.Code), item.Count))
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardBreakdownItemDalDto>> BuildPropertyTicketBreakdownAsync(
        IQueryable<App.Domain.Ticket> tickets,
        CancellationToken cancellationToken)
    {
        var counts = await tickets
            .Where(ticket => ticket.PropertyId != null)
            .GroupBy(ticket => new { ticket.PropertyId, ticket.Property!.Slug })
            .Select(group => new { group.Key.PropertyId, group.Key.Slug, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var propertyIds = counts
            .Where(item => item.PropertyId.HasValue)
            .Select(item => item.PropertyId!.Value)
            .ToList();
        var labels = await _dbContext.Properties
            .AsNoTracking()
            .Where(property => propertyIds.Contains(property.Id))
            .Select(property => new { property.Id, Label = property.Label.ToString() })
            .ToDictionaryAsync(item => item.Id, item => item.Label, cancellationToken);

        return counts
            .Select(item => Breakdown(
                item.Slug,
                item.PropertyId.HasValue ? labels.GetValueOrDefault(item.PropertyId.Value) : null,
                item.Count))
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardBreakdownItemDalDto>> BuildUnitsByFloorBreakdownAsync(
        IQueryable<App.Domain.Unit> units,
        CancellationToken cancellationToken)
    {
        var counts = await units
            .GroupBy(unit => unit.FloorNr)
            .Select(group => new { FloorNr = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return counts
            .OrderBy(item => item.FloorNr ?? int.MinValue)
            .Select(item =>
            {
                var label = item.FloorNr?.ToString() ?? string.Empty;
                return Breakdown(item.FloorNr?.ToString() ?? "unknown", label, item.Count);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardBreakdownItemDalDto>> BuildContactTypeBreakdownAsync(
        IQueryable<App.Domain.ResidentContact> contacts,
        CancellationToken cancellationToken)
    {
        var counts = await contacts
            .GroupBy(contact => contact.Contact!.ContactType!.Code)
            .Select(group => new { Code = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var codes = counts.Select(item => item.Code).ToList();
        var labels = await _dbContext.ContactTypes
            .AsNoTracking()
            .Where(contactType => codes.Contains(contactType.Code))
            .Select(contactType => new { contactType.Code, Label = contactType.Label.ToString() })
            .ToDictionaryAsync(item => item.Code, item => item.Label, cancellationToken);

        return counts
            .Select(item => Breakdown(item.Code, labels.GetValueOrDefault(item.Code), item.Count))
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardMetricDalDto>> BuildTicketMetricsAsync(
        IQueryable<App.Domain.Ticket> tickets,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken)
    {
        return new List<DashboardMetricDalDto>
        {
            Metric("openTickets", await OpenTickets(tickets, options).CountAsync(cancellationToken)),
            Metric("overdueTickets", await OverdueTickets(tickets, options).CountAsync(cancellationToken)),
            Metric("highPriorityTickets", await HighPriorityTickets(tickets, options).CountAsync(cancellationToken)),
            Metric("dueNext7Days", await OpenTickets(tickets, options).CountAsync(ticket =>
                ticket.DueAt != null
                && ticket.DueAt >= options.TodayStartUtc
                && ticket.DueAt < options.NextSevenDaysEndUtc, cancellationToken))
        };
    }

    private static async Task<IReadOnlyList<DashboardMetricDalDto>> BuildWorkMetricsAsync(
        IQueryable<App.Domain.ScheduledWork> work,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken)
    {
        return new List<DashboardMetricDalDto>
        {
            Metric("scheduledToday", await work.CountAsync(item =>
                item.ScheduledStart >= options.TodayStartUtc
                && item.ScheduledStart < options.TomorrowStartUtc, cancellationToken)),
            Metric("scheduledNext7Days", await work.CountAsync(item =>
                item.ScheduledStart >= options.TodayStartUtc
                && item.ScheduledStart < options.NextSevenDaysEndUtc, cancellationToken)),
            Metric("delayedWork", await DelayedWork(work, options).CountAsync(cancellationToken))
        };
    }

    private static IQueryable<DashboardTicketPreviewDalDto> ProjectTickets(IQueryable<App.Domain.Ticket> tickets) =>
        tickets.Select(ticket => new DashboardTicketPreviewDalDto
        {
            TicketId = ticket.Id,
            TicketNr = ticket.TicketNr,
            Title = ticket.Title.ToString(),
            StatusCode = ticket.TicketStatus!.Code,
            StatusLabel = ticket.TicketStatus.Label.ToString(),
            PriorityCode = ticket.TicketPriority!.Code,
            PriorityLabel = ticket.TicketPriority.Label.ToString(),
            DueAt = ticket.DueAt,
            CreatedAt = ticket.CreatedAt,
            ClosedAt = ticket.ClosedAt,
            CustomerSlug = ticket.Customer == null ? null : ticket.Customer.Slug,
            CustomerName = ticket.Customer == null ? null : ticket.Customer.Name,
            PropertySlug = ticket.Property == null ? null : ticket.Property.Slug,
            PropertyName = ticket.Property == null ? null : ticket.Property.Label.ToString(),
            UnitSlug = ticket.Unit == null ? null : ticket.Unit.Slug,
            UnitNr = ticket.Unit == null ? null : ticket.Unit.UnitNr,
            ResidentIdCode = ticket.Resident == null ? null : ticket.Resident.IdCode,
            ResidentName = ticket.Resident == null ? null : (ticket.Resident.FirstName + " " + ticket.Resident.LastName).Trim()
        });

    private static IQueryable<DashboardWorkPreviewDalDto> ProjectWork(IQueryable<App.Domain.ScheduledWork> work) =>
        work.Select(item => new DashboardWorkPreviewDalDto
        {
            ScheduledWorkId = item.Id,
            TicketId = item.TicketId,
            TicketNr = item.Ticket!.TicketNr,
            TicketTitle = item.Ticket.Title.ToString(),
            VendorName = item.Vendor!.Name,
            WorkStatusCode = item.WorkStatus!.Code,
            WorkStatusLabel = item.WorkStatus.Label.ToString(),
            ScheduledStart = item.ScheduledStart,
            ScheduledEnd = item.ScheduledEnd,
            RealStart = item.RealStart,
            RealEnd = item.RealEnd
        });

    private static IQueryable<DashboardLeasePreviewDalDto> ProjectLeases(IQueryable<App.Domain.Lease> leases) =>
        leases.Select(lease => new DashboardLeasePreviewDalDto
        {
            LeaseId = lease.Id,
            CustomerSlug = lease.Unit!.Property!.Customer!.Slug,
            CustomerName = lease.Unit.Property.Customer.Name,
            PropertySlug = lease.Unit.Property.Slug,
            PropertyName = lease.Unit.Property.Label.ToString(),
            UnitSlug = lease.Unit.Slug,
            UnitNr = lease.Unit.UnitNr,
            ResidentIdCode = lease.Resident!.IdCode,
            ResidentName = (lease.Resident.FirstName + " " + lease.Resident.LastName).Trim(),
            RoleCode = lease.LeaseRole!.Code,
            RoleLabel = lease.LeaseRole.Label.ToString(),
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            CreatedAt = lease.CreatedAt
        });

    private static IQueryable<DashboardRepresentativePreviewDalDto> ProjectRepresentatives(
        IQueryable<App.Domain.CustomerRepresentative> representatives) =>
        representatives.Select(representative => new DashboardRepresentativePreviewDalDto
        {
            RepresentativeId = representative.Id,
            ResidentIdCode = representative.Resident!.IdCode,
            ResidentName = (representative.Resident.FirstName + " " + representative.Resident.LastName).Trim(),
            RoleCode = representative.CustomerRepresentativeRole!.Code,
            RoleLabel = representative.CustomerRepresentativeRole.Label.ToString(),
            ValidFrom = representative.ValidFrom,
            ValidTo = representative.ValidTo,
            CustomerSlug = representative.Customer == null ? null : representative.Customer.Slug,
            CustomerName = representative.Customer == null ? null : representative.Customer.Name
        });

    private IQueryable<DashboardUnitPreviewDalDto> ProjectUnitPreview(
        IQueryable<App.Domain.Unit> units,
        PortalDashboardQueryOptionsDalDto options)
    {
        var closedCodes = options.OpenTicketExcludedStatusCodes.ToList();
        return units.Select(unit => new DashboardUnitPreviewDalDto
        {
            UnitId = unit.Id,
            UnitSlug = unit.Slug,
            UnitNr = unit.UnitNr,
            FloorNr = unit.FloorNr,
            SizeM2 = unit.SizeM2,
            HasActiveLease = unit.Leases!.Any(lease =>
                lease.StartDate <= options.TodayDate
                && (lease.EndDate == null || lease.EndDate >= options.TodayDate)),
            CurrentResidentName = unit.Leases!
                .Where(lease => lease.StartDate <= options.TodayDate
                                && (lease.EndDate == null || lease.EndDate >= options.TodayDate))
                .OrderByDescending(lease => lease.StartDate)
                .Select(lease => (lease.Resident!.FirstName + " " + lease.Resident.LastName).Trim())
                .FirstOrDefault(),
            OpenTicketCount = unit.Tickets!.Count(ticket => !closedCodes.Contains(ticket.TicketStatus!.Code))
        });
    }

    private IQueryable<CustomerDashboardContextDalDto> CustomerContext(Guid managementCompanyId, Guid customerId) =>
        _dbContext.Customers.AsNoTracking()
            .Where(customer => customer.ManagementCompanyId == managementCompanyId && customer.Id == customerId)
            .Select(customer => new CustomerDashboardContextDalDto
            {
                ManagementCompanyId = managementCompanyId,
                CompanySlug = customer.ManagementCompany!.Slug,
                CompanyName = customer.ManagementCompany.Name,
                CustomerId = customer.Id,
                CustomerSlug = customer.Slug,
                CustomerName = customer.Name,
                RegistryCode = customer.RegistryCode,
                BillingEmail = customer.BillingEmail,
                Phone = customer.Phone
            });

    private IQueryable<PropertyDashboardContextDalDto> PropertyContext(Guid managementCompanyId, Guid customerId, Guid propertyId) =>
        _dbContext.Properties.AsNoTracking()
            .Where(property => property.Id == propertyId
                               && property.CustomerId == customerId
                               && property.Customer!.ManagementCompanyId == managementCompanyId)
            .Select(property => new PropertyDashboardContextDalDto
            {
                ManagementCompanyId = managementCompanyId,
                CompanySlug = property.Customer!.ManagementCompany!.Slug,
                CompanyName = property.Customer.ManagementCompany.Name,
                CustomerId = property.CustomerId,
                CustomerSlug = property.Customer.Slug,
                CustomerName = property.Customer.Name,
                RegistryCode = property.Customer.RegistryCode,
                BillingEmail = property.Customer.BillingEmail,
                Phone = property.Customer.Phone,
                PropertyId = property.Id,
                PropertySlug = property.Slug,
                PropertyName = property.Label.ToString(),
                PropertyTypeLabel = property.PropertyType!.Label.ToString(),
                AddressLine = property.AddressLine,
                City = property.City,
                PostalCode = property.PostalCode
            });

    private IQueryable<UnitDashboardContextDalDto> UnitContext(Guid managementCompanyId, Guid propertyId, Guid unitId) =>
        _dbContext.Units.AsNoTracking()
            .Where(unit => unit.Id == unitId
                           && unit.PropertyId == propertyId
                           && unit.Property!.Customer!.ManagementCompanyId == managementCompanyId)
            .Select(unit => new UnitDashboardContextDalDto
            {
                ManagementCompanyId = managementCompanyId,
                CompanySlug = unit.Property!.Customer!.ManagementCompany!.Slug,
                CompanyName = unit.Property.Customer.ManagementCompany.Name,
                CustomerId = unit.Property.CustomerId,
                CustomerSlug = unit.Property.Customer.Slug,
                CustomerName = unit.Property.Customer.Name,
                RegistryCode = unit.Property.Customer.RegistryCode,
                BillingEmail = unit.Property.Customer.BillingEmail,
                Phone = unit.Property.Customer.Phone,
                PropertyId = unit.PropertyId,
                PropertySlug = unit.Property.Slug,
                PropertyName = unit.Property.Label.ToString(),
                PropertyTypeLabel = unit.Property.PropertyType!.Label.ToString(),
                AddressLine = unit.Property.AddressLine,
                City = unit.Property.City,
                PostalCode = unit.Property.PostalCode,
                UnitId = unit.Id,
                UnitSlug = unit.Slug,
                UnitNr = unit.UnitNr,
                FloorNr = unit.FloorNr,
                SizeM2 = unit.SizeM2
            });

    private IQueryable<ResidentDashboardContextDalDto> ResidentContext(Guid managementCompanyId, Guid residentId) =>
        _dbContext.Residents.AsNoTracking()
            .Where(resident => resident.ManagementCompanyId == managementCompanyId && resident.Id == residentId)
            .Select(resident => new ResidentDashboardContextDalDto
            {
                ManagementCompanyId = managementCompanyId,
                CompanySlug = resident.ManagementCompany!.Slug,
                CompanyName = resident.ManagementCompany.Name,
                ResidentId = resident.Id,
                ResidentIdCode = resident.IdCode,
                FirstName = resident.FirstName,
                LastName = resident.LastName,
                FullName = (resident.FirstName + " " + resident.LastName).Trim(),
                PreferredLanguage = resident.PreferredLanguage
            });

    private async Task<IReadOnlyList<DashboardRecentActivityDalDto>> BuildManagementRecentActivityAsync(
        Guid managementCompanyId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken)
    {
        var customers = await _dbContext.Customers.AsNoTracking()
            .Where(customer => customer.ManagementCompanyId == managementCompanyId && customer.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(customer => customer.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(customer => new DashboardRecentActivityDalDto
            {
                ItemType = "customer",
                Label = customer.Name,
                SupportingText = customer.RegistryCode,
                EventAt = customer.CreatedAt,
                CustomerSlug = customer.Slug
            })
            .ToListAsync(cancellationToken);
        var properties = await _dbContext.Properties.AsNoTracking()
            .Where(property => property.Customer!.ManagementCompanyId == managementCompanyId && property.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(property => property.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(property => new DashboardRecentActivityDalDto
            {
                ItemType = "property",
                Label = property.Label.ToString(),
                SupportingText = property.Customer!.Name,
                EventAt = property.CreatedAt,
                CustomerSlug = property.Customer.Slug,
                PropertySlug = property.Slug
            })
            .ToListAsync(cancellationToken);
        var units = await _dbContext.Units.AsNoTracking()
            .Where(unit => unit.Property!.Customer!.ManagementCompanyId == managementCompanyId && unit.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(unit => unit.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(unit => new DashboardRecentActivityDalDto
            {
                ItemType = "unit",
                Label = unit.UnitNr,
                SupportingText = unit.Property!.Label.ToString(),
                EventAt = unit.CreatedAt,
                CustomerSlug = unit.Property.Customer!.Slug,
                PropertySlug = unit.Property.Slug,
                UnitSlug = unit.Slug
            })
            .ToListAsync(cancellationToken);
        var residents = await _dbContext.Residents.AsNoTracking()
            .Where(resident => resident.ManagementCompanyId == managementCompanyId && resident.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(resident => resident.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(resident => new DashboardRecentActivityDalDto
            {
                ItemType = "resident",
                Label = (resident.FirstName + " " + resident.LastName).Trim(),
                SupportingText = resident.IdCode,
                EventAt = resident.CreatedAt,
                ResidentIdCode = resident.IdCode
            })
            .ToListAsync(cancellationToken);
        var tickets = await ProjectTickets(CompanyTickets(managementCompanyId).Where(ticket => ticket.CreatedAt >= options.RecentSinceUtc))
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(ticket => new DashboardRecentActivityDalDto
            {
                ItemType = "ticket",
                Label = ticket.TicketNr,
                SupportingText = ticket.Title,
                EventAt = ticket.CreatedAt,
                CustomerSlug = ticket.CustomerSlug,
                PropertySlug = ticket.PropertySlug,
                UnitSlug = ticket.UnitSlug,
                ResidentIdCode = ticket.ResidentIdCode,
                TicketId = ticket.TicketId
            })
            .ToListAsync(cancellationToken);

        return customers.Concat(properties).Concat(units).Concat(residents).Concat(tickets)
            .OrderByDescending(item => item.EventAt)
            .Take(options.PreviewLimit)
            .ToList();
    }

    private async Task<IReadOnlyList<DashboardRecentActivityDalDto>> BuildCustomerRecentActivityAsync(
        Guid managementCompanyId,
        Guid customerId,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken)
    {
        var properties = await _dbContext.Properties.AsNoTracking()
            .Where(property => property.CustomerId == customerId
                               && property.Customer!.ManagementCompanyId == managementCompanyId
                               && property.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(property => property.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(property => new DashboardRecentActivityDalDto
            {
                ItemType = "property",
                Label = property.Label.ToString(),
                SupportingText = property.AddressLine,
                EventAt = property.CreatedAt,
                CustomerSlug = property.Customer!.Slug,
                PropertySlug = property.Slug
            })
            .ToListAsync(cancellationToken);
        var units = await _dbContext.Units.AsNoTracking()
            .Where(unit => unit.Property!.CustomerId == customerId
                           && unit.Property.Customer!.ManagementCompanyId == managementCompanyId
                           && unit.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(unit => unit.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(unit => new DashboardRecentActivityDalDto
            {
                ItemType = "unit",
                Label = unit.UnitNr,
                SupportingText = unit.Property!.Label.ToString(),
                EventAt = unit.CreatedAt,
                CustomerSlug = unit.Property.Customer!.Slug,
                PropertySlug = unit.Property.Slug,
                UnitSlug = unit.Slug
            })
            .ToListAsync(cancellationToken);
        var leases = await _dbContext.Leases.AsNoTracking()
            .Where(lease => lease.Unit!.Property!.CustomerId == customerId
                            && lease.Unit.Property.Customer!.ManagementCompanyId == managementCompanyId
                            && lease.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(lease => lease.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(lease => new DashboardRecentActivityDalDto
            {
                ItemType = "lease",
                Label = (lease.Resident!.FirstName + " " + lease.Resident.LastName).Trim(),
                SupportingText = lease.Unit!.UnitNr,
                EventAt = lease.CreatedAt,
                CustomerSlug = lease.Unit.Property!.Customer!.Slug,
                PropertySlug = lease.Unit.Property.Slug,
                UnitSlug = lease.Unit.Slug,
                ResidentIdCode = lease.Resident.IdCode
            })
            .ToListAsync(cancellationToken);
        var tickets = await ProjectTickets(CompanyTickets(managementCompanyId)
                .Where(ticket => ticket.CustomerId == customerId && ticket.CreatedAt >= options.RecentSinceUtc))
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(ticket => new DashboardRecentActivityDalDto
            {
                ItemType = "ticket",
                Label = ticket.TicketNr,
                SupportingText = ticket.Title,
                EventAt = ticket.CreatedAt,
                CustomerSlug = ticket.CustomerSlug,
                PropertySlug = ticket.PropertySlug,
                UnitSlug = ticket.UnitSlug,
                ResidentIdCode = ticket.ResidentIdCode,
                TicketId = ticket.TicketId
            })
            .ToListAsync(cancellationToken);

        return properties.Concat(units).Concat(leases).Concat(tickets)
            .OrderByDescending(item => item.EventAt)
            .Take(options.PreviewLimit)
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardTimelineItemDalDto>> BuildUnitTimelineAsync(
        IQueryable<App.Domain.Ticket> tickets,
        IQueryable<App.Domain.Lease> leases,
        IQueryable<App.Domain.ScheduledWork> work,
        PortalDashboardQueryOptionsDalDto options,
        CancellationToken cancellationToken)
    {
        var leaseStarts = await leases
            .Where(lease => lease.CreatedAt >= options.RecentSinceUtc)
            .OrderByDescending(lease => lease.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(lease => new DashboardTimelineItemDalDto
            {
                ItemType = "lease",
                Label = (lease.Resident!.FirstName + " " + lease.Resident.LastName).Trim(),
                SupportingText = lease.LeaseRole!.Label.ToString(),
                EventAt = lease.CreatedAt,
                LeaseId = lease.Id
            })
            .ToListAsync(cancellationToken);
        var ticketEvents = await tickets
            .Where(ticket => ticket.CreatedAt >= options.RecentSinceUtc || ticket.ClosedAt >= options.RecentSinceUtc)
            .OrderByDescending(ticket => ticket.ClosedAt ?? ticket.CreatedAt)
            .Take(options.PreviewLimit)
            .Select(ticket => new DashboardTimelineItemDalDto
            {
                ItemType = ticket.ClosedAt == null ? "ticketCreated" : "ticketClosed",
                Label = ticket.TicketNr,
                SupportingText = ticket.Title.ToString(),
                EventAt = ticket.ClosedAt ?? ticket.CreatedAt,
                TicketId = ticket.Id
            })
            .ToListAsync(cancellationToken);
        var workEvents = await work
            .Where(item => item.RealEnd != null && item.RealEnd >= options.RecentSinceUtc)
            .OrderByDescending(item => item.RealEnd)
            .Take(options.PreviewLimit)
            .Select(item => new DashboardTimelineItemDalDto
            {
                ItemType = "workCompleted",
                Label = item.Ticket!.TicketNr,
                SupportingText = item.Vendor!.Name,
                EventAt = item.RealEnd ?? item.CreatedAt,
                TicketId = item.TicketId,
                ScheduledWorkId = item.Id
            })
            .ToListAsync(cancellationToken);

        return leaseStarts.Concat(ticketEvents).Concat(workEvents)
            .OrderByDescending(item => item.EventAt)
            .Take(options.PreviewLimit)
            .ToList();
    }

    private static DashboardMetricDalDto Metric(string key, int value) => new()
    {
        Key = key,
        Value = value
    };

    private static DashboardBreakdownItemDalDto Breakdown(string? code, string? label, int count) => new()
    {
        Code = code ?? string.Empty,
        Label = string.IsNullOrWhiteSpace(label) ? code ?? string.Empty : label,
        Count = count
    };
}
