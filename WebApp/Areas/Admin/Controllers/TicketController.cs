using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Areas_Admin_Controllers
{
    [Area("Admin")]
    [Authorize (Roles = "SystemAdmin")]
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;

        public TicketController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Ticket
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Tickets.Include(t => t.Customer).Include(t => t.ManagementCompany).Include(t => t.Property).Include(t => t.Resident).Include(t => t.TicketCategory).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.Unit).Include(t => t.Vendor);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Ticket/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.ManagementCompany)
                .Include(t => t.Property)
                .Include(t => t.Resident)
                .Include(t => t.TicketCategory)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.Unit)
                .Include(t => t.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Ticket/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address");
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine");
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName");
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code");
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Code");
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Code");
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr");
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name");
            return View();
        }

        // POST: Ticket/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TicketNr,CreatedAt,DueAt,ClosedAt,ManagementCompanyId,CustomerId,ResidentId,PropertyId,UnitId,TicketCategoryId,VendorId,TicketStatusId,TicketPriorityId,Id")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                var title = Request.Form[nameof(Ticket.Title)].ToString();
                var description = Request.Form[nameof(Ticket.Description)].ToString();

                ticket.Id = Guid.NewGuid();
                ticket.Title = title;
                ticket.Description = description;
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", ticket.CustomerId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", ticket.ManagementCompanyId);
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine", ticket.PropertyId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", ticket.ResidentId);
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code", ticket.TicketCategoryId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Code", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Code", ticket.TicketStatusId);
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr", ticket.UnitId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", ticket.VendorId);
            return View(ticket);
        }

        // GET: Ticket/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", ticket.CustomerId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", ticket.ManagementCompanyId);
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine", ticket.PropertyId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", ticket.ResidentId);
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code", ticket.TicketCategoryId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Code", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Code", ticket.TicketStatusId);
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr", ticket.UnitId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", ticket.VendorId);
            return View(ticket);
        }

        // POST: Ticket/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("TicketNr,CreatedAt,DueAt,ClosedAt,ManagementCompanyId,CustomerId,ResidentId,PropertyId,UnitId,TicketCategoryId,VendorId,TicketStatusId,TicketPriorityId,Id")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Tickets.FindAsync(id);
                if (entity == null) return NotFound();

                var title = Request.Form[nameof(Ticket.Title)].ToString();
                var description = Request.Form[nameof(Ticket.Description)].ToString();

                entity.TicketNr = ticket.TicketNr;
                entity.CreatedAt = ticket.CreatedAt;
                entity.DueAt = ticket.DueAt;
                entity.ClosedAt = ticket.ClosedAt;
                entity.ManagementCompanyId = ticket.ManagementCompanyId;
                entity.CustomerId = ticket.CustomerId;
                entity.ResidentId = ticket.ResidentId;
                entity.PropertyId = ticket.PropertyId;
                entity.UnitId = ticket.UnitId;
                entity.TicketCategoryId = ticket.TicketCategoryId;
                entity.VendorId = ticket.VendorId;
                entity.TicketStatusId = ticket.TicketStatusId;
                entity.TicketPriorityId = ticket.TicketPriorityId;

                entity.Title.SetTranslation(title);
                entity.Description.SetTranslation(description);

                try
                {
                    _context.Update(entity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", ticket.CustomerId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", ticket.ManagementCompanyId);
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine", ticket.PropertyId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", ticket.ResidentId);
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code", ticket.TicketCategoryId);
            ViewData["TicketPriorityId"] = new SelectList(_context.TicketPriorities, "Id", "Code", ticket.TicketPriorityId);
            ViewData["TicketStatusId"] = new SelectList(_context.TicketStatuses, "Id", "Code", ticket.TicketStatusId);
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr", ticket.UnitId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", ticket.VendorId);
            return View(ticket);
        }

        // GET: Ticket/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.ManagementCompany)
                .Include(t => t.Property)
                .Include(t => t.Resident)
                .Include(t => t.TicketCategory)
                .Include(t => t.TicketPriority)
                .Include(t => t.TicketStatus)
                .Include(t => t.Unit)
                .Include(t => t.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Ticket/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(Guid id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }
    }
}
