using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;

namespace WebApp.Areas_Admin_Controllers
{
    [Area("Admin")]
    public class ScheduledWorkController : Controller
    {
        private readonly AppDbContext _context;

        public ScheduledWorkController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ScheduledWork
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.ScheduledWorks.Include(s => s.Ticket).Include(s => s.Vendor).Include(s => s.WorkStatus);
            return View(await appDbContext.ToListAsync());
        }

        // GET: ScheduledWork/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheduledWork = await _context.ScheduledWorks
                .Include(s => s.Ticket)
                .Include(s => s.Vendor)
                .Include(s => s.WorkStatus)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (scheduledWork == null)
            {
                return NotFound();
            }

            return View(scheduledWork);
        }

        // GET: ScheduledWork/Create
        public IActionResult Create()
        {
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description");
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name");
            ViewData["WorkStatusId"] = new SelectList(_context.WorkStatuses, "Id", "Code");
            return View();
        }

        // POST: ScheduledWork/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ScheduledStart,ScheduledEnd,RealStart,RealEnd,Notes,CreatedAt,VendorId,TicketId,WorkStatusId,Id")] ScheduledWork scheduledWork)
        {
            if (ModelState.IsValid)
            {
                scheduledWork.Id = Guid.NewGuid();
                _context.Add(scheduledWork);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", scheduledWork.TicketId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", scheduledWork.VendorId);
            ViewData["WorkStatusId"] = new SelectList(_context.WorkStatuses, "Id", "Code", scheduledWork.WorkStatusId);
            return View(scheduledWork);
        }

        // GET: ScheduledWork/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheduledWork = await _context.ScheduledWorks.FindAsync(id);
            if (scheduledWork == null)
            {
                return NotFound();
            }
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", scheduledWork.TicketId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", scheduledWork.VendorId);
            ViewData["WorkStatusId"] = new SelectList(_context.WorkStatuses, "Id", "Code", scheduledWork.WorkStatusId);
            return View(scheduledWork);
        }

        // POST: ScheduledWork/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ScheduledStart,ScheduledEnd,RealStart,RealEnd,Notes,CreatedAt,VendorId,TicketId,WorkStatusId,Id")] ScheduledWork scheduledWork)
        {
            if (id != scheduledWork.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(scheduledWork);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduledWorkExists(scheduledWork.Id))
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
            ViewData["TicketId"] = new SelectList(_context.Tickets, "Id", "Description", scheduledWork.TicketId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", scheduledWork.VendorId);
            ViewData["WorkStatusId"] = new SelectList(_context.WorkStatuses, "Id", "Code", scheduledWork.WorkStatusId);
            return View(scheduledWork);
        }

        // GET: ScheduledWork/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scheduledWork = await _context.ScheduledWorks
                .Include(s => s.Ticket)
                .Include(s => s.Vendor)
                .Include(s => s.WorkStatus)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (scheduledWork == null)
            {
                return NotFound();
            }

            return View(scheduledWork);
        }

        // POST: ScheduledWork/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var scheduledWork = await _context.ScheduledWorks.FindAsync(id);
            if (scheduledWork != null)
            {
                _context.ScheduledWorks.Remove(scheduledWork);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ScheduledWorkExists(Guid id)
        {
            return _context.ScheduledWorks.Any(e => e.Id == id);
        }
    }
}
