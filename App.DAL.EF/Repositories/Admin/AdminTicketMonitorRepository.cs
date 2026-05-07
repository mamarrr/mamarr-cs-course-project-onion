using App.DAL.Contracts.Repositories.Admin;
using App.DAL.DTO.Admin.Tickets;
using App.DAL.EF.Mappers.Admin;
using Microsoft.EntityFrameworkCore;

namespace App.DAL.EF.Repositories.Admin;

public class AdminTicketMonitorRepository : IAdminTicketMonitorRepository
{
    private readonly AppDbContext _dbContext;
    private readonly AdminTicketMonitorDalMapper _mapper;

    public AdminTicketMonitorRepository(AppDbContext dbContext, AdminTicketMonitorDalMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AdminTicketListItemDalDto>> SearchTicketsAsync(AdminTicketSearchDalDto search, CancellationToken cancellationToken = default)
    {
        var query = TicketQuery();

        if (!string.IsNullOrWhiteSpace(search.Company))
        {
            var company = search.Company.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.ManagementCompany != null && ticket.ManagementCompany.Name.ToUpper().Contains(company));
        }

        if (!string.IsNullOrWhiteSpace(search.Customer))
        {
            var customer = search.Customer.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.Customer != null && ticket.Customer.Name.ToUpper().Contains(customer));
        }

        if (!string.IsNullOrWhiteSpace(search.TicketNumber))
        {
            var ticketNumber = search.TicketNumber.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.TicketNr.ToUpper().Contains(ticketNumber));
        }

        if (!string.IsNullOrWhiteSpace(search.Status))
        {
            var status = search.Status.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.TicketStatus != null && ticket.TicketStatus.Code.ToUpper().Contains(status));
        }

        if (!string.IsNullOrWhiteSpace(search.Priority))
        {
            var priority = search.Priority.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.TicketPriority != null && ticket.TicketPriority.Code.ToUpper().Contains(priority));
        }

        if (!string.IsNullOrWhiteSpace(search.Category))
        {
            var category = search.Category.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.TicketCategory != null && ticket.TicketCategory.Code.ToUpper().Contains(category));
        }

        if (!string.IsNullOrWhiteSpace(search.Vendor))
        {
            var vendor = search.Vendor.Trim().ToUpperInvariant();
            query = query.Where(ticket => ticket.Vendor != null && ticket.Vendor.Name.ToUpper().Contains(vendor));
        }

        if (search.CreatedFrom.HasValue)
        {
            query = query.Where(ticket => ticket.CreatedAt >= search.CreatedFrom.Value);
        }

        if (search.CreatedTo.HasValue)
        {
            query = query.Where(ticket => ticket.CreatedAt <= search.CreatedTo.Value);
        }

        if (search.DueFrom.HasValue)
        {
            query = query.Where(ticket => ticket.DueAt >= search.DueFrom.Value);
        }

        if (search.DueTo.HasValue)
        {
            query = query.Where(ticket => ticket.DueAt <= search.DueTo.Value);
        }

        if (search.OverdueOnly)
        {
            query = query.Where(ticket => ticket.DueAt != null && ticket.DueAt < DateTime.UtcNow && ticket.ClosedAt == null);
        }

        if (search.OpenOnly)
        {
            query = query.Where(ticket => ticket.ClosedAt == null);
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync(cancellationToken);

        return tickets.Select(_mapper.MapListItem).ToList();
    }

    public async Task<AdminTicketDetailsDalDto?> GetTicketDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await TicketQuery()
            .Include(ticket => ticket.Property)
            .Include(ticket => ticket.Unit)
            .Include(ticket => ticket.Resident)
            .Include(ticket => ticket.ScheduledWorks!)
                .ThenInclude(work => work.Vendor)
            .Include(ticket => ticket.ScheduledWorks!)
                .ThenInclude(work => work.WorkStatus)
            .Include(ticket => ticket.ScheduledWorks!)
                .ThenInclude(work => work.WorkLogs!)
                    .ThenInclude(log => log.AppUser)
            .FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var listItem = _mapper.MapListItem(ticket);
        var scheduledWorks = ticket.ScheduledWorks?
            .OrderBy(work => work.ScheduledStart)
            .Select(work => new AdminScheduledWorkDalDto
            {
                Id = work.Id,
                VendorName = work.Vendor?.Name ?? string.Empty,
                WorkStatusLabel = work.WorkStatus?.Label.ToString() ?? string.Empty,
                ScheduledStart = work.ScheduledStart,
                ScheduledEnd = work.ScheduledEnd,
                RealStart = work.RealStart,
                RealEnd = work.RealEnd
            })
            .ToList() ?? [];

        var workLogs = ticket.ScheduledWorks?
            .SelectMany(work => work.WorkLogs ?? Enumerable.Empty<App.Domain.WorkLog>())
            .OrderByDescending(log => log.CreatedAt)
            .Select(log => new AdminWorkLogDalDto
            {
                Id = log.Id,
                LoggedBy = $"{log.AppUser?.FirstName} {log.AppUser?.LastName}".Trim(),
                CreatedAt = log.CreatedAt,
                WorkStart = log.WorkStart,
                WorkEnd = log.WorkEnd,
                Hours = log.Hours,
                MaterialCost = log.MaterialCost,
                LaborCost = log.LaborCost,
                Description = log.Description?.ToString()
            })
            .ToList() ?? [];

        return new AdminTicketDetailsDalDto
        {
            Id = listItem.Id,
            TicketNumber = listItem.TicketNumber,
            Title = listItem.Title,
            CompanyName = listItem.CompanyName,
            CustomerName = listItem.CustomerName,
            StatusCode = listItem.StatusCode,
            StatusLabel = listItem.StatusLabel,
            PriorityLabel = listItem.PriorityLabel,
            CategoryLabel = listItem.CategoryLabel,
            VendorName = listItem.VendorName,
            CreatedAt = listItem.CreatedAt,
            DueAt = listItem.DueAt,
            ClosedAt = listItem.ClosedAt,
            IsOverdue = listItem.IsOverdue,
            Description = ticket.Description.ToString(),
            PropertyLabel = ticket.Property?.Label.ToString(),
            UnitNumber = ticket.Unit?.UnitNr,
            ResidentName = ticket.Resident == null ? null : $"{ticket.Resident.FirstName} {ticket.Resident.LastName}".Trim(),
            ScheduledWorks = scheduledWorks,
            WorkLogs = workLogs
        };
    }

    private IQueryable<App.Domain.Ticket> TicketQuery()
    {
        return _dbContext.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.ManagementCompany)
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.TicketStatus)
            .Include(ticket => ticket.TicketPriority)
            .Include(ticket => ticket.TicketCategory)
            .Include(ticket => ticket.Vendor);
    }
}
