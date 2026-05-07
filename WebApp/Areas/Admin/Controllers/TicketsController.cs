using App.BLL.Contracts;
using App.BLL.DTO.Admin.Tickets;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Admin.Tickets;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SystemAdmin")]
[Route("Admin/Tickets")]
public class TicketsController : Controller
{
    private readonly IAppBLL _bll;

    public TicketsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(AdminTicketSearchViewModel search, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminTicketMonitor.SearchTicketsAsync(new AdminTicketSearchDto
        {
            Company = search.Company,
            Customer = search.Customer,
            TicketNumber = search.TicketNumber,
            Status = search.Status,
            Priority = search.Priority,
            Category = search.Category,
            Vendor = search.Vendor,
            CreatedFrom = search.CreatedFrom,
            CreatedTo = search.CreatedTo,
            DueFrom = search.DueFrom,
            DueTo = search.DueTo,
            OverdueOnly = search.OverdueOnly,
            OpenOnly = search.OpenOnly
        }, cancellationToken);

        return View(new AdminTicketListViewModel
        {
            PageTitle = AdminText.TicketsMonitor,
            ActiveSection = "Tickets",
            Search = search,
            Tickets = dto.Tickets.Select(ticket => new AdminTicketListItemViewModel
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                CompanyName = ticket.CompanyName,
                CustomerName = ticket.CustomerName,
                StatusLabel = ticket.StatusLabel,
                PriorityLabel = ticket.PriorityLabel,
                CategoryLabel = ticket.CategoryLabel,
                VendorName = ticket.VendorName,
                CreatedAt = ticket.CreatedAt,
                DueAt = ticket.DueAt,
                IsOverdue = ticket.IsOverdue
            }).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminTicketMonitor.GetTicketDetailsAsync(id, cancellationToken);
        if (dto is null) return NotFound();

        return View(new AdminTicketDetailsViewModel
        {
            PageTitle = dto.TicketNumber,
            ActiveSection = "Tickets",
            Id = dto.Id,
            TicketNumber = dto.TicketNumber,
            Title = dto.Title,
            Description = dto.Description,
            CompanyName = dto.CompanyName,
            CustomerName = dto.CustomerName,
            PropertyLabel = dto.PropertyLabel,
            UnitNumber = dto.UnitNumber,
            ResidentName = dto.ResidentName,
            StatusLabel = dto.StatusLabel,
            PriorityLabel = dto.PriorityLabel,
            CategoryLabel = dto.CategoryLabel,
            VendorName = dto.VendorName,
            CreatedAt = dto.CreatedAt,
            DueAt = dto.DueAt,
            ClosedAt = dto.ClosedAt,
            IsOverdue = dto.IsOverdue,
            ScheduledWorks = dto.ScheduledWorks.Select(work => new AdminScheduledWorkViewModel
            {
                VendorName = work.VendorName,
                WorkStatusLabel = work.WorkStatusLabel,
                ScheduledStart = work.ScheduledStart,
                ScheduledEnd = work.ScheduledEnd,
                RealStart = work.RealStart,
                RealEnd = work.RealEnd
            }).ToList(),
            WorkLogs = dto.WorkLogs.Select(log => new AdminWorkLogViewModel
            {
                LoggedBy = log.LoggedBy,
                CreatedAt = log.CreatedAt,
                Hours = log.Hours,
                MaterialCost = log.MaterialCost,
                LaborCost = log.LaborCost,
                Description = log.Description
            }).ToList()
        });
    }
}
